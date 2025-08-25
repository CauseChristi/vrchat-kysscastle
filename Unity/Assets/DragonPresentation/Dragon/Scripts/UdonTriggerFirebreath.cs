
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UdonTriggerFirebreath : UdonSharpBehaviour
{
	
		[Header("Particle systems to control")]
    public ParticleSystem[] particleSystems;
		
    void OnEnable()
    {
			foreach (ParticleSystem ps in particleSystems) {
					if (ps != null) {
							var emission = ps.emission;
							emission.enabled = true;
					}
			}
    }
		
		void OnDisable()
    {
			foreach (ParticleSystem ps in particleSystems) {
					if (ps != null) {
							var emission = ps.emission;
							emission.enabled = false;
					}
			}
    }
}
