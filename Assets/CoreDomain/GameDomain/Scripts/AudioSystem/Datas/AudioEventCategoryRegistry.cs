using UnityEngine;

namespace CoreDomain.GameDomain.Scripts.AudioSystem.Datas
{
    /// <summary>
    /// Um "pedaco" do catalogo de sons, dedicado a uma categoria (Passos,
    /// Portas, Objetos, etc). Crie um asset desses por categoria e registre-o
    /// no AudioEventRegistry mestre. Times diferentes podem editar categorias
    /// diferentes sem conflitar no mesmo arquivo.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/Audio Event Category Registry", fileName = "New Category Registry")]
    public class AudioEventCategoryRegistry : ScriptableObject
    {
        [Tooltip("So para identificacao no Editor - nao afeta a logica.")]
        [SerializeField] private string categoryName = "Nova Categoria";

        [SerializeField] private AudioEventEntry[] entries;

        public string CategoryName => categoryName;

        public AudioEvent GetById(AudioEventId id)
        {
            foreach (var entry in entries)
            {
                if (entry.id == id)
                    return entry.audioEvent;
            }
            
            return null;
        }

        // Util para o registry mestre detectar Ids duplicados entre categorias.
        public bool Contains(AudioEventId id)
        {
            foreach (var entry in entries)
            {
                if (entry.id == id)
                    return true;
            }

            return false;
        }
    }
}