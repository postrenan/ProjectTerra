SAHARA_COUNTRIES = {"Algeria", "Libya", "Chad", "Niger", "Mali", "Mauritania", "Sudan", "Western Sahara", "Egypt"}
ANDES_COUNTRIES = {"Bolivia", "Peru", "Ecuador", "Colombia", "Argentina", "Chile"}

def evaluate_region_infrastructure(r, coastal_ids, river_map, lake_map, port_map, airport_map):
    rid = r["id"]
    rname = r["name"]
    country = r["country"]
    lat = r["centerLat"]
    lon = r["centerLon"]
    rname_lower = rname.lower()
    country_lower = country.lower()

    has_coast = rid in coastal_ids
    has_rivers = rid in river_map and len(river_map[rid]) > 0
    has_lakes = rid in lake_map and len(lake_map[rid]) > 0
    has_ports = rid in port_map and len(port_map[rid]) > 0
    has_airports = rid in airport_map and len(airport_map[rid]) > 0
    has_roads = True

    is_sahara_zone = (country in SAHARA_COUNTRIES) and (14.0 <= lat <= 32.0) and (-17.0 <= lon <= 35.0)
    is_deep_sahara = is_sahara_zone and not has_coast and not ("Nile" in river_map.get(rid, set()))

    is_andes_zone = (country in ANDES_COUNTRIES) and (-55.0 <= lat <= 11.0) and (-80.0 <= lon <= -64.0)
    is_inland_andes = is_andes_zone and not has_coast and not has_lakes

    is_antarctica = ("antarctica" in country_lower or "antarctica" in rname_lower or lat < -60.0)
    is_greenland_ice = ("greenland" in country_lower and lat > 66.0)
    is_polar_extreme = is_antarctica or is_greenland_ice or (abs(lat) > 76.0)

    is_arabian_empty_quarter = (country in {"Saudi Arabia", "Oman", "United Arab Emirates"}) and (17.0 <= lat <= 24.0) and (46.0 <= lon <= 55.0) and not has_coast
    is_atacama_core = ("atacama" in rname_lower or "antofagasta" in rname_lower or "tarapaca" in rname_lower) and not has_coast and not has_rivers

    # 1. Agriculture
    if is_polar_extreme:
        is_agri_viable = False
        agri_reason = "Calota glacial polar permanente com permafrost congelado (agricultura inviável)."
    elif is_deep_sahara:
        is_agri_viable = False
        agri_reason = "Deserto hiperárido do Saara com ausência de recursos hídricos e solo arenoso."
    elif is_arabian_empty_quarter:
        is_agri_viable = False
        agri_reason = "Dunas hiperáridas do deserto Rub' al Khali sem solo arável."
    elif is_atacama_core:
        is_agri_viable = False
        agri_reason = "Núcleo hiperárido do Deserto do Atacama sem precipitação pluviométrica."
    elif is_inland_andes and (lat < -15.0 and lat > -25.0 and "potos" in rname_lower):
        is_agri_viable = True
        agri_reason = "Agricultura de altitude e pastoreio andino (cultivo de batata, quinoa e grãos resistentes)."
    else:
        is_agri_viable = True
        agri_reason = "Solos cultiváveis e regime climático favoráveis à atividade agrícola."

    # 2. Fishing
    if is_deep_sahara:
        is_fishing_viable = False
        fishing_reason = "Indisponível no Deserto do Saara: Região árida sem litoral, rios ou lagos navegáveis."
    elif is_inland_andes:
        is_fishing_viable = False
        fishing_reason = "Indisponível na Cordilheira dos Andes: Relevo montanhoso de alta altitude sem corpos hídricos navegáveis."
    elif is_arabian_empty_quarter or is_atacama_core:
        is_fishing_viable = False
        fishing_reason = "Indisponível em deserto interior hiperárido sem fontes aquíferas superficiais."
    elif is_antarctica:
        is_fishing_viable = has_coast
        fishing_reason = "Pesca oceânica austral e caça científica litorânea." if has_coast else "Inviável na calota de gelo interior da Antártida."
    else:
        if has_coast:
            is_fishing_viable = True
            fishing_reason = "Litoral com acesso direto a águas oceânicas e cais pesqueiro comercial."
        elif has_lakes:
            is_fishing_viable = True
            lake_n = list(lake_map[rid])[0]
            fishing_reason = f"Pesca de água doce e entreposto ribeirinho ({lake_n})."
        elif has_rivers:
            is_fishing_viable = True
            river_n = list(river_map[rid])[0]
            fishing_reason = f"Pesca fluvial e cais de atracação em bacia hidrográfica ({river_n})."
        else:
            is_fishing_viable = False
            fishing_reason = "Indisponível: Província interior sem acesso a litoral marítimo, lagos ou rios navegáveis."

    # 3. Aviation
    if is_antarctica:
        is_aviation_viable = True
        aviation_reason = "Pista de pouso e aeródromo de pesquisa polar sobre pista de gelo compactado."
    elif is_polar_extreme and not has_airports:
        is_aviation_viable = False
        aviation_reason = "Região remota sem aeródromo ou condições para aviação civil."
    else:
        is_aviation_viable = True
        aviation_reason = "Malha aérea operacional com aeroporto comercial ou pista de pouso executiva."

    # 4. Trucking
    if is_antarctica:
        is_trucking_viable = False
        trucking_reason = "Inexistência de malha rodoviária na calota de gelo antártica."
    elif is_greenland_ice:
        is_trucking_viable = False
        trucking_reason = "Inexistência de malha viária sobre a calota glacial da Groenlândia."
    else:
        is_trucking_viable = True
        trucking_reason = "Malha viária pavimentada e conexões intermunicipais para frete rodoviário."

    # Names & Category
    water_name = "Oceano / Mar Aberto" if has_coast else ("Bacia Hidrográfica" if has_rivers else ("Grande Lago" if has_lakes else "Interior Continental"))
    if has_lakes and len(lake_map[rid]) > 0:
        water_name = f"Lago {list(lake_map[rid])[0]}"
    elif has_rivers and len(river_map[rid]) > 0:
        water_name = f"Rio {list(river_map[rid])[0]}"
    elif has_coast:
        water_name = "Costa Oceânica / Mar Territorial"

    port_name = port_map[rid][0] if has_ports else ("Terminal Marítimo Costeiro" if has_coast else ("Entreposto Fluvial" if (has_rivers or has_lakes) else "Sem Instalação Portuária"))
    airport_name = airport_map[rid][0] if has_airports else ("Aeródromo Regional Pavimentado" if is_aviation_viable else "Sem Pista de Pouso")

    if is_polar_extreme:
        category = "Gelo Polar & Tundra Ártica"
    elif is_deep_sahara:
        category = "Deserto Hiperárido (Saara)"
    elif is_inland_andes:
        category = "Cordilheira dos Andes (Alta Montanha)"
    elif is_arabian_empty_quarter or is_atacama_core:
        category = "Deserto Árido Interior"
    elif has_coast:
        category = "Zona Costeira Litorânea"
    elif has_rivers or has_lakes:
        category = "Bacia Fluvial & Lacustre"
    else:
        category = "Planície Continental & Terras Altas"

    return {
        "regionId": rid,
        "regionName": rname,
        "countryName": country,
        "hasCoastline": bool(has_coast),
        "hasRivers": bool(has_rivers),
        "hasLakes": bool(has_lakes),
        "waterBodyName": water_name,
        "hasPort": bool(has_ports or has_coast or ((has_rivers or has_lakes) and is_fishing_viable)),
        "portName": port_name,
        "hasAirport": bool(has_airports or is_aviation_viable),
        "airportName": airport_name,
        "hasRoadNetwork": bool(has_roads and is_trucking_viable),
        "isAgricultureViable": bool(is_agri_viable),
        "agricultureReason": agri_reason,
        "isFishingViable": bool(is_fishing_viable),
        "fishingReason": fishing_reason,
        "isAviationViable": bool(is_aviation_viable),
        "aviationReason": aviation_reason,
        "isTruckingViable": bool(is_trucking_viable),
        "truckingReason": trucking_reason,
        "environmentCategory": category
    }
