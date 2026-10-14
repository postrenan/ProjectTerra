# -*- coding: utf-8 -*-
"""
Politica de tamanho minimo de regiao do Project Terra.

Resolve o problema de granularidade inconsistente da camada admin_1 do Natural
Earth (ex.: 26 cantoes suicos de ~876 km2 de mediana ao lado de estados
brasileiros de ~226.000 km2). A regra usa DOIS limiares independentes:

  1. COUNTRY_MIN_KM2 : um pais so e subdividido se ele proprio for grande o
     suficiente. Abaixo disso o pais inteiro vira UMA regiao, nao importa
     quantas divisoes tenha (resolve Suica, Luxemburgo, Belgica, Holanda...).
  2. DIVISION_MIN_KM2 : dentro dos paises subdivididos, cada divisao abaixo
     deste piso e absorvida pela divisao vizinha de maior area (remove enclaves
     e distritos minusculos que sobrevivem em paises grandes).

IMPORTANTE: esta politica NAO renumera nem remove IDs de regiao. Ela apenas
decide, para cada divisao, qual ID final pinta seus pixels no mapa de selecao
(region_id_map) e quais fronteiras internas sao desenhadas. Os registros do
banco (regions_database.bin) e todos os dados indexados por regionId
(heightmaps, cidades, infraestrutura) permanecem validos; divisoes absorvidas
simplesmente deixam de ser clicaveis, como ja acontece hoje com o poligono-base
dos paises cobertos por estados.
"""
import math

# --- Limiares (ajustaveis) ---
COUNTRY_MIN_KM2 = 150_000   # area minima do pais para que ele seja subdividido
DIVISION_MIN_KM2 = 5_000    # area minima de uma divisao para virar regiao propria

R_EARTH_KM = 6371.0088


def latlon_to_xy(lon, lat, width, height):
    x = (lon + 180.0) / 360.0 * (width - 1)
    y = (90.0 - lat) / 180.0 * (height - 1)
    return x, y


def _ring_area_km2(ring):
    """Area geodesica (excesso esferico) de um anel lon/lat, em km2."""
    n = len(ring)
    if n < 4:
        return 0.0
    total = 0.0
    for i in range(n):
        lon1, lat1 = ring[i][0], ring[i][1]
        lon2, lat2 = ring[(i + 1) % n][0], ring[(i + 1) % n][1]
        total += math.radians(lon2 - lon1) * (
            2 + math.sin(math.radians(lat1)) + math.sin(math.radians(lat2))
        )
    return abs(total * R_EARTH_KM * R_EARTH_KM / 2.0)


def _poly_area_km2(poly):
    """poly = [anel_externo, buraco, buraco...]"""
    if not poly:
        return 0.0
    a = _ring_area_km2(poly[0])
    for hole in poly[1:]:
        a -= _ring_area_km2(hole)
    return max(0.0, a)


def geom_area_km2(geom):
    if not geom:
        return 0.0
    t = geom.get('type')
    c = geom.get('coordinates', [])
    if t == 'Polygon':
        return _poly_area_km2(c)
    if t == 'MultiPolygon':
        return sum(_poly_area_km2(p) for p in c)
    return 0.0


def geom_centroid(geom):
    """Centroide aproximado (media dos vertices do maior anel externo)."""
    if not geom:
        return (0.0, 0.0)
    t = geom.get('type')
    c = geom.get('coordinates', [])
    rings = []
    if t == 'Polygon' and c:
        rings = [c[0]]
    elif t == 'MultiPolygon':
        rings = [p[0] for p in c if p]
    best = None
    best_n = -1
    for r in rings:
        if len(r) > best_n:
            best_n = len(r)
            best = r
    if not best:
        return (0.0, 0.0)
    sx = sum(p[0] for p in best) / len(best)
    sy = sum(p[1] for p in best) / len(best)
    return (sx, sy)


def _state_centroid(feat):
    p = feat.get('properties', {}) or {}
    lon = p.get('longitude')
    lat = p.get('latitude')
    if lon not in (None, 0) and lat not in (None, 0):
        return (float(lon), float(lat))
    return geom_centroid(feat.get('geometry'))


def compute_merge(country_feats, state_feats,
                  country_min=COUNTRY_MIN_KM2, division_min=DIVISION_MIN_KM2):
    """
    Reproduz o esquema de IDs do build (paises: 1..N na ordem do arquivo;
    estados: N+i+1 na ordem do arquivo) e aplica a politica de fusao.

    Retorna um dict com:
      n_countries      : numero de paises (offset dos estados)
      country_area     : {country_id: area_km2}
      state_area       : {state_id: area_km2}
      state_final_id   : {state_id: id_final_que_pinta_os_pixels}
      kept_state_ids   : set de estados que viram regiao propria
      subdivided_cids  : set de country_ids subdivididos
      country_key      : {region_id_final: chave_de_pais}  (p/ classificar fronteira
                         nacional vs interna; paises -> proprio id; estados -> chave
                         do grupo adm0_a3)
    """
    n_countries = len(country_feats)

    # --- areas e chave de pais por ISO3 ---
    country_area = {}
    name_to_cid = {}
    a3_to_cid = {}
    for i, feat in enumerate(country_feats):
        cid = i + 1
        p = feat.get('properties', {}) or {}
        country_area[cid] = geom_area_km2(feat.get('geometry'))
        nm = (p.get('NAME') or p.get('ADMIN') or '').strip().lower()
        a3 = (p.get('ADM0_A3') or '').strip().upper()
        if nm:
            name_to_cid.setdefault(nm, cid)
        if a3 and a3 != '-99':
            a3_to_cid.setdefault(a3, cid)

    # --- agrupar estados por pais (chave adm0_a3; fallback admin) ---
    state_area = {}
    state_cid = {}      # state_id -> country_id (ou None se grupo sem pais-base)
    state_a3 = {}       # state_id -> chave do grupo (adm0_a3 ou admin)
    state_cent = {}
    groups = {}         # chave_grupo -> [state_id,...]

    for i, feat in enumerate(state_feats):
        sid = n_countries + i + 1
        p = feat.get('properties', {}) or {}
        area = geom_area_km2(feat.get('geometry'))
        state_area[sid] = area
        state_cent[sid] = _state_centroid(feat)

        a3 = (p.get('adm0_a3') or '').strip().upper()
        admin = (p.get('admin') or '').strip().lower()
        cid = a3_to_cid.get(a3)
        if cid is None and admin:
            cid = name_to_cid.get(admin)
        gkey = a3 if (a3 and a3 != '-99') else ('ADMIN:' + admin)
        state_cid[sid] = cid
        state_a3[sid] = gkey
        groups.setdefault(gkey, []).append(sid)

    # --- decidir subdivisao e IDs finais ---
    state_final_id = {}
    kept_state_ids = set()
    subdivided_cids = set()
    country_key = {}
    for cid in country_area:
        country_key[cid] = cid  # pais -> propria chave nacional

    pseudo = 20000  # chaves nacionais sinteticas p/ grupos sem pais-base
    for gkey, sids in groups.items():
        cid = state_cid[sids[0]]
        area_pais = country_area.get(cid) if cid else sum(state_area[s] for s in sids)
        kept = [s for s in sids if state_area[s] >= division_min]
        subdivided = (area_pais is not None and area_pais >= country_min and len(kept) >= 2)

        # chave nacional do grupo (para classificar fronteiras)
        if cid is not None:
            gnat = cid
        else:
            gnat = pseudo
            pseudo += 1

        if subdivided:
            if cid is not None:
                subdivided_cids.add(cid)
            # vizinho mais proximo entre os mantidos (por centroide)
            for s in sids:
                if state_area[s] >= division_min:
                    state_final_id[s] = s
                    kept_state_ids.add(s)
                    country_key[s] = gnat
                else:
                    cx, cy = state_cent[s]
                    best, bestd = kept[0], None
                    for k in kept:
                        kx, ky = state_cent[k]
                        d = (kx - cx) ** 2 + (ky - cy) ** 2
                        if bestd is None or d < bestd:
                            bestd, best = d, k
                    state_final_id[s] = best  # pixels herdam o vizinho mantido
            # garantir chave nacional dos mantidos
            for k in kept:
                country_key[k] = gnat
        else:
            if cid is not None:
                # absorve tudo no poligono-base do pais
                for s in sids:
                    state_final_id[s] = cid
            else:
                # grupo sem pais-base (ex.: UMI): colapsa no maior membro
                rep = max(sids, key=lambda s: state_area[s])
                country_key[rep] = gnat
                for s in sids:
                    state_final_id[s] = rep
                kept_state_ids.add(rep)

    return {
        'n_countries': n_countries,
        'country_area': country_area,
        'state_area': state_area,
        'state_final_id': state_final_id,
        'kept_state_ids': kept_state_ids,
        'subdivided_cids': subdivided_cids,
        'country_key': country_key,
    }
