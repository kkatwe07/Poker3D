using UnityEngine;

namespace view
{
    public class CardView : MonoBehaviour
    {
        [SerializeField] private Renderer cardRenderer;

        // Assign texture (card face or back)
        public void SetTexture(Texture tex)
        {
            if (cardRenderer == null)
            {
                cardRenderer = GetComponent<Renderer>();
            }

            cardRenderer.material.mainTexture = tex;
        }
    }
}