using UnityEngine;
using UnityEditor;
using System.Collections;

// UpdateBakerySHMode 
// Gathers all the materials used in the scene, 
// and then sets their Bakery directional mode. 

namespace SilentTools
{
public class UpdateBakerySHMode : ScriptableWizard
{
	// Show only the currently supported types, but match to Bakery's list in ftRenderLightmaps.cs.
    private enum BakeryRenderDirMode
    {
        None = 0,
        BakedNormalMaps = 1,
        DominantDirection = 2,
        RNM = 3,
        SH = 4,
        MonoSH = 6
    };

	private enum BakeryVertexLMMode
	{
		NoChange = 0,
		Enable = 1,
		Disable = 2
	};

	[SerializeField]
	private BakeryRenderDirMode newDirectionalMode;
	[SerializeField]
	private BakeryVertexLMMode newVertexMode;
	
	[MenuItem("Tools/Silent/Update Material Directional Mode for Bakery")]

	static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<UpdateBakerySHMode>  ("Update Material Directional Mode for Bakery", "Update");
	}

	private void ClearBakeryKeywords(Material m)
	{
		m.DisableKeyword("_BAKERY_RNM");
		m.DisableKeyword("_BAKERY_SH");
		m.DisableKeyword("_BAKERY_MONOSH");
		m.SetFloat("_Bakery", 0);
	}

	void OnWizardCreate()
	{
		int totalMaterials = 0;
		int validMaterials = 0;
		int updatedMaterials = 0;

		Renderer[] renderers = GameObject.FindSceneObjectsOfType (typeof(Renderer)) as Renderer[];
		foreach (var renderer in renderers) 
		{
			foreach (Material m in renderer.sharedMaterials) 
			{
				totalMaterials++;
				// Probably only works with Filamented or its templates. Sorry!
				if (m != null && m.HasProperty("_Bakery")) 
				{
                	float originalBakeryValue = m.GetFloat("_Bakery");

					switch(newDirectionalMode)
					{
						// Placeholders, but clearing is important. 
						case (BakeryRenderDirMode.None):
						ClearBakeryKeywords(m);
						break;
						case (BakeryRenderDirMode.BakedNormalMaps):
						ClearBakeryKeywords(m);
						break;
						case (BakeryRenderDirMode.DominantDirection):
						ClearBakeryKeywords(m);
						break;

						// Note: Matched to [KeywordEnum(None, SH, RNM, MonoSH)] in Filamented
						case (BakeryRenderDirMode.SH):
						ClearBakeryKeywords(m);
						m.EnableKeyword("_BAKERY_SH");
						m.SetFloat("_Bakery", 1);
						break;
						
						case (BakeryRenderDirMode.RNM):
						ClearBakeryKeywords(m);
						m.EnableKeyword("_BAKERY_RNM");
						m.SetFloat("_Bakery", 2);
						break;
						
						case (BakeryRenderDirMode.MonoSH):
						ClearBakeryKeywords(m);
						m.EnableKeyword("_BAKERY_MONOSH");
						m.SetFloat("_Bakery", 3);
						break;

					}


                    switch (newVertexMode)
                    {
                        case BakeryVertexLMMode.NoChange:
                            break;
                        case BakeryVertexLMMode.Enable:
                            m.EnableKeyword("_BAKERY_VERTEXLM");
                            m.SetFloat("_BakeryVertexLM", 1);
                            break;
                        case BakeryVertexLMMode.Disable:
                            m.DisableKeyword("_BAKERY_VERTEXLM");
                            m.SetFloat("_BakeryVertexLM", 0);
                            break;
                    }
					
					validMaterials++;
					if (m.GetFloat("_Bakery") != originalBakeryValue)
					{
						updatedMaterials++;
					}
				}
			}

		}
		Debug.Log("Updated " + updatedMaterials + " materials of " + totalMaterials + ".");
	}
}
}