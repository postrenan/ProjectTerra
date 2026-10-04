using UnityEngine;

namespace ProjectTerra.Core
{
    /// <summary>
    /// Base para qualquer sistema que guarde posições world-space em cache (listas de
    /// segmentos, centros de lagos, destinos de IA, camadas do mapa tático).
    ///
    /// <para>
    /// <see cref="FloatingOrigin.Rebase"/> translada os GameObjects raiz da cena, mas
    /// não consegue corrigir dados que vivem apenas na memória. Um rebase de 25 km
    /// (o <c>threshold</c> padrão) deixaria qualquer cache desses deslocado para sempre.
    /// </para>
    ///
    /// <para>
    /// Exemplos reais que já quebraram por causa disso: relva brotando sobre as
    /// estradas, barcos perdendo empuxo no oceano, animais caminhando para um ponto
    /// 25 km distante, e as cidades desaparecendo do marcador no mapa tático.
    /// </para>
    ///
    /// <para>
    /// Basta herdar e implementar <see cref="OnOriginRebased"/>; a inscrição e a
    /// limpeza do evento ficam por conta da base, o que impede o vazamento típico de
    /// delegate preso a um componente destruído.
    /// </para>
    /// </summary>
    public abstract class OriginRebasedBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Quantos rebaseamentos este objeto já absorveu. Útil para diagnóstico.
        /// </summary>
        public int RebaseCount { get; private set; }

        protected virtual void OnEnable()
        {
            FloatingOrigin.OnOriginRebased += HandleRebase;
        }

        protected virtual void OnDisable()
        {
            FloatingOrigin.OnOriginRebased -= HandleRebase;
        }

        /// <summary>
        /// Chamado com o vetor de translação aplicado ao mundo. Todos os valores
        /// world-space guardados em cache devem ser deslocados por <paramref name="offset"/>.
        /// </summary>
        protected abstract void OnOriginRebased(Vector3 offset);

        private void HandleRebase(Vector3 offset)
        {
            RebaseCount++;
            OnOriginRebased(offset);
        }

        /// <summary>
        /// Versão para dados estáticos/compartilhados, quando não há um MonoBehaviour
        /// por instância para servir de âncora. Use dentro do mesmo callback de rebase.
        /// </summary>
        protected static void RebasePoint(ref Vector3 cachedWorldPoint, Vector3 offset)
        {
            cachedWorldPoint -= offset;
        }
    }
}