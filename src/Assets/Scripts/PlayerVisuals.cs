using UnityEngine;

namespace SpaceShooter
{
    /// <summary>
    /// Owns the visual side of the ship that lives outside ShipFactory:
    ///  • engine flame trail
    ///  • re-tint when the level palette changes
    /// Attached to Player root by ShipFactory.
    /// </summary>
    public class PlayerVisuals : MonoBehaviour
    {
        public Material[] coloredMaterials;
        public Material engineMaterial;
        public TrailRenderer engineTrail;

        public void Apply(Palette palette)
        {
            if (coloredMaterials != null)
            {
                for (int i = 0; i < coloredMaterials.Length; i++)
                {
                    var m = coloredMaterials[i];
                    if (m == null) continue;
                    if (m.HasProperty("_Color")) m.SetColor("_Color", palette.ShipColor);
                    if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", palette.ShipEmissive);
                }
            }
            if (engineMaterial != null)
            {
                if (engineMaterial.HasProperty("_Color")) engineMaterial.SetColor("_Color", palette.TorpedoColor);
                if (engineMaterial.HasProperty("_EmissionColor")) engineMaterial.SetColor("_EmissionColor", palette.TorpedoEmissive);
            }
            if (engineTrail != null)
            {
                engineTrail.startColor = palette.TorpedoEmissive;
                engineTrail.endColor   = new Color(palette.TorpedoColor.r, palette.TorpedoColor.g, palette.TorpedoColor.b, 0f);
            }
        }
    }
}
