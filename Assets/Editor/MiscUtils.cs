using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition.Attributes;
using UnityEngine.SceneManagement;

namespace TimeGhost
{
    static class MiscUtils
    {
        // A one-off re-write of animation height curves (moving the entire world 1300m up)
        //[MenuItem("Tools/Patch Camera Curves")]
        static void PatchCurves()
        {
            const string kPropertyName = "m_LocalPosition.y";
            
            foreach (AnimationClip clip in Selection.objects)
            {
                Debug.Log($"Patching curve {kPropertyName} in clip {clip.name}");
                
                var curveBinding = AnimationUtility.GetCurveBindings(clip).Single(binding => binding.propertyName == kPropertyName);
                
                var curve = AnimationUtility.GetEditorCurve(clip, curveBinding);
                var keys = curve.keys;
                for (var i = 0; i < keys.Length; ++i)
                {
                    keys[i].value += 1300f;
                }
                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clip, curveBinding, curve);
            }
        }

        [MenuItem("Tools/Find HiPoly Contributors")]
        static void FindHiPolyContributors()
        {
            var meshes = 
            EditorSceneManager.GetAllScenes()
                .SelectMany(s => s.GetRootGameObjects())
                .SelectMany(go => go.GetComponentsInChildren<MeshRenderer>())
                .Where(mr => GameObjectUtility.AreStaticEditorFlagsSet(mr.gameObject, StaticEditorFlags.ContributeGI))
                .Select(mr => mr.GetComponent<MeshFilter>())
                .Where(mf => mf != null)
                .Select(mf => (mf.sharedMesh, mf.gameObject))
                .Where(m => m.sharedMesh != null)
                .OrderByDescending(m => m.sharedMesh.vertexCount)
                .ToArray();

            foreach (var m in meshes.Take(20))
            {
                Debug.Log($"{m.sharedMesh.name}/{m.gameObject.name} {m.sharedMesh.vertexCount}", m.gameObject);
            }
        }
        
        /// /// /// /// /// /// ///
        // HUGE HACKS TO BAKE SOME PARTS OF THE TERRAIN TO A TEXTURED MESH
        [MenuItem("Tools/Terrain To Mesh")]
        public static void TerrainToMesh()
        {
            var terrain = Selection.activeGameObject.GetComponent<Terrain>();
            var terrainTranslation = terrain.transform.position;
            var terrainSize = terrain.terrainData.size;

            // destructive changes
            terrain.gameObject.layer = 31;
            terrain.shadowCastingMode = ShadowCastingMode.Off;
            //terrain.heightmapMaximumLOD = 2;
            //terrain.basemapDistance = 0f;
            //terrain.terrainData.heightmapResolution = 1025;
            var tyTerrainEditor =
                typeof(UnityEditor.TerrainTools.TerrainInspectorUtility).Assembly.GetType(
                    "UnityEditor.TerrainInspector");
            var terrainEditor = Editor.CreateEditor(terrain, tyTerrainEditor);
            try
            {
                terrainEditor.OnInspectorGUI();
            } catch(System.Exception){}
            var miResize = tyTerrainEditor.GetMethod("ResizeHeightmap", BindingFlags.Instance|BindingFlags.NonPublic);
            var miMarkDirty = tyTerrainEditor.GetMethod("MarkTerrainDataDirty", BindingFlags.Instance|BindingFlags.NonPublic);
            miResize.Invoke(terrainEditor, new object[] { 1025 });
            miMarkDirty.Invoke(terrainEditor, null);

            // Terrain mesh setup
            //var tyTerrainToMesh = typeof(UnityEngine.Rendering.UnifiedRayTracing.BackendHelpers).Assembly.GetType("UnityEngine.Rendering.UnifiedRayTracing.TerrainToMesh");
            //var asm = CompilationPipeline.GetAssemblies().First(asm => asm.name == "Unity.Rendering.LightTransport.Runtime");
            // foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
            // {
            //     Debug.Log(a.GetName().Name);
            // }
            //
            var asm = System.AppDomain.CurrentDomain.GetAssemblies().First(asm => asm.GetName().Name == "Unity.Rendering.LightTransport.Runtime");
            var tyTerrainToMesh = asm.GetType("UnityEngine.Rendering.UnifiedRayTracing.TerrainToMesh");
            var miConvert = tyTerrainToMesh.GetMethods(BindingFlags.Static|BindingFlags.Public).Single(mi => mi.Name == "Convert" && mi.GetParameters().Length == 1);
            //var mesh = UnityEngine.Rendering.UnifiedRayTracing.TerrainToMesh.Convert(terrain);
            var mesh = (Mesh)miConvert.Invoke(null, new [] { terrain });
            //mesh.hideFlags = HideFlags.DontSave;

            var terrainMap = new Texture2D(2048, 2048);

            var mat = new Material(Shader.Find("HDRP/Lit"));
            //mat.hideFlags = HideFlags.DontSave;
            mat.SetTexture("_BaseColorMap", terrainMap);
            mat.SetFloat("_Smoothness", 0.1f);
            HDMaterial.ValidateMaterial(mat);

            var goTerrainAsMesh = new GameObject("terrain_as_mesh");
            //go.hideFlags = HideFlags.DontSave;
            SceneManager.MoveGameObjectToScene(goTerrainAsMesh, terrain.gameObject.scene);

            goTerrainAsMesh.transform.position = terrainTranslation;

            var mf = goTerrainAsMesh.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = goTerrainAsMesh.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.receiveGI = ReceiveGI.LightProbes;
            GameObjectUtility.SetStaticEditorFlags(goTerrainAsMesh, StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);

            // Render setup
            var camGO = new GameObject();
            //camGO.hideFlags = HideFlags.DontSave;
            SceneManager.MoveGameObjectToScene(camGO, terrain.gameObject.scene);
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Color;
            cam.backgroundColor = Color.red;
            cam.useOcclusionCulling = false;
            cam.cullingMask = 1 << 31;
            cam.orthographic = true;
            var camPos = terrainTranslation + new Vector3(terrainSize.x / 2f, terrainSize.y, terrainSize.z / 2f);
            var camRot = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            cam.transform.SetPositionAndRotation(camPos, camRot);
            cam.aspect = 1f;
            cam.orthographicSize = terrainSize.x / 2f;
            cam.nearClipPlane = 0f;
            cam.farClipPlane = terrainSize.y;

            var rt = RenderTexture.GetTemporary(terrainMap.width, terrainMap.height, 32);
            cam.targetTexture = rt;
            var tmpRT = RTHandles.Alloc(cam.pixelWidth, cam.pixelHeight);

            {
                var aovRequest = AOVRequest.NewDefault();
                aovRequest.SetFullscreenOutput(MaterialSharedProperty.Albedo);

                var aovRequestBuilder = new AOVRequestBuilder();
                aovRequestBuilder.Add(aovRequest,
                    bufferId => tmpRT,
                    null,
                    new[] { AOVBuffers.Color },
                    null,
                    bufferId => tmpRT,
                    (cmd, textures, customPassTextures, properties) =>
                    {
                        if (textures.Count > 0)
                        {
                            RenderTexture.active = textures[0].rt;
                            terrainMap.ReadPixels(new Rect(0, 0, cam.pixelWidth, cam.pixelHeight), 0, 0, false);
                            terrainMap.Apply();
                            RenderTexture.active = null;

                            var mesh = goTerrainAsMesh.GetComponent<MeshFilter>().sharedMesh;
                            var mat = goTerrainAsMesh.GetComponent<MeshRenderer>().sharedMaterial;
                            var tex = mat.GetTexture("_BaseColorMap") as Texture2D;

                            var basePath = terrain.gameObject.scene.path;
                            Debug.Log(basePath);

                            // if(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh)))
                            // {
                            //     AssetDatabase.CreateAsset(mesh, "Assets/terrainmesh.asset");
                            // }
                            //
                            // if(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mat)))
                            // {
                            //     AssetDatabase.CreateAsset(mat, "Assets/terrainmat.mat");
                            // }
                            //
                            // if(string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tex)))
                            // {
                            //     AssetDatabase.CreateAsset(tex, "Assets/terrainmap.asset");
                            // }

                        }
                    });

                var aovRequestDataCollection = aovRequestBuilder.Build();

                var hdCameraData = camGO.AddComponent<HDAdditionalCameraData>();
                hdCameraData.SetAOVRequests(aovRequestDataCollection);
            }

            camGO.GetComponent<Camera>().Render();
            SceneView.RepaintAll();

            EditorApplication.QueuePlayerLoopUpdate();

            EditorApplication.delayCall += () =>
            {
                EditorApplication.QueuePlayerLoopUpdate();

                EditorApplication.delayCall += () =>
                {
                    RenderTexture.active = tmpRT;
                    terrainMap.ReadPixels(new Rect(0f, 0f, cam.pixelWidth, cam.pixelHeight), 0, 0);
                    terrainMap.Apply();
                    RenderTexture.active = null;
                    //RTHandles.Release(tmpRT);

                    //Object.DestroyImmediate(camGO);
                    cam.enabled = false;

                    terrain.gameObject.SetActive(false);
                };
            };
        }
    }
}
