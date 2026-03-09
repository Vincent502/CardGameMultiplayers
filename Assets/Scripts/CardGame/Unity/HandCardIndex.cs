using UnityEngine;

namespace CardGame.Unity
{
    /// <summary>Stocke l'index de la carte dans la main (pour retrouver la carte après réorganisation par SetAsLastSibling).</summary>
    public class HandCardIndex : MonoBehaviour
    {
        public int Index;
    }
}
