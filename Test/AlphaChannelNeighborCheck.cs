using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace GRUProject.Firepropagation
{
    public class AlphaChannelNeighborCheck : MonoBehaviour
    {
        public ComputeShader computeShader;
        public RenderTexture renderTexture;
        private int kernelHandle;

        [SerializeField]
        [Header("Keep this value low 8 = 8^3 = 512, 16^3 = 4096 voxels")]
        private int _XDensity = 8;
        [SerializeField]
        private int _YDensity = 8;
        [SerializeField]
        private int _ZDensity = 8;
        [SerializeField]
        private Bounds _Bounds = new Bounds(Vector3.zero, Vector3.one);
        [SerializeField]
        private Transform _Root;

        [HideInInspector]
        public List<FireVoxel> _fireVoxels;
        [SerializeField, HideInInspector]
        private List<FireVoxel> _VoxelMap;
        private Voxeliser _voxeliser;

        private void Awake()
        {
            RenderTexture rt = new RenderTexture(_XDensity, _YDensity, _ZDensity, RenderTextureFormat.ARGB32);
            rt.enableRandomWrite = true;
            rt.Create();
            renderTexture = rt;
            RunAndCreate();
        }
        public ComputeShader voxelEditShader;
        public void SetPixelColor(int x, int y, int z, Color setColor)
        {
            var kernelHandle = voxelEditShader.FindKernel("CSMain");
            // Set the RenderTexture as the active texture for the compute shader.
            voxelEditShader.SetTexture(kernelHandle, "Result", renderTexture);

            // Pass the color data to the shader.
            voxelEditShader.SetVector("Color", setColor);

            // Dispatch the shader to edit the single voxel.
            voxelEditShader.Dispatch(kernelHandle, x, y, z);

            // No need for the rest of the logic involving Texture3D and Graphics.Blit, the compute shader handles everything.
        }
        public ComputeShader transferShader;

        public void TransferRenderTextureToTexture3D(RenderTexture rTex, Texture3D tex3D)
        {
            int kernelHandle = transferShader.FindKernel("CSTransfer");

            transferShader.SetTexture(kernelHandle, "InputTexture3D", rTex);
            transferShader.SetTexture(kernelHandle, "OutputTexture3D", tex3D);

            int threadGroupsX = Mathf.CeilToInt(rTex.width / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(rTex.height / 8.0f);
            transferShader.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, rTex.depth);
        }
        [ContextMenu("Run and create voxels")]
        private void RunAndCreate()
        {
            Debug.Log("run");
            Run();
            for (int x = 0; x < _XDensity; x++)
            {
                for (int y = 0; y < _YDensity; y++)
                {
                    for (int z = 0; z < _ZDensity; z++)
                    {
                        if (_voxeliser.VoxelMap[x][y][z].IsAble == 1)
                        {
                            _fireVoxels.Add(_voxeliser.VoxelMap[x][y][z]);
                            Color color = Color.clear;
                            color.r = _voxeliser.VoxelMap[x][y][z].WorldPosition.x;
                            color.g = _voxeliser.VoxelMap[x][y][z].WorldPosition.y;
                            color.b = _voxeliser.VoxelMap[x][y][z].WorldPosition.z;
                            color.a = 0;
                            SetPixelColor(x, y, z, color);
                        }
                        _VoxelMap.Add(_voxeliser.VoxelMap[x][y][z]);
                    }
                }
            }
        }

        private void Start()
        {
            kernelHandle = computeShader.FindKernel("CSMain");
            computeShader.SetTexture(kernelHandle, "InputTexture", renderTexture);
            computeShader.SetTexture(kernelHandle, "OutputTexture", renderTexture);

            // Setting a specific pixel's alpha to 1 as an example
            //    SetPixelColor(10, 10, 10, Color.clear);

            // Call the compute shader
            computeShader.Dispatch(kernelHandle, renderTexture.width / 8, renderTexture.height / 8, renderTexture.depth / 8);
        }
        private void Run()
        {
            _voxeliser = new Voxeliser(_Bounds, _XDensity, _YDensity, _ZDensity);
            _voxeliser.Voxelize(_Root);
        }
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(transform.position + _Bounds.center, _Bounds.size);

            var gridCubeSize = new Vector3(
                _Bounds.size.x / _XDensity,
                _Bounds.size.y / _YDensity,
                _Bounds.size.z / _ZDensity);
            var worldCentre = _Bounds.min + gridCubeSize / 2;


            foreach (var fireVoxel in _VoxelMap.Where(fireVoxel => fireVoxel.IsAble == 1))
            {
                //       Gizmos.DrawWireCube(_VoxelMap[((x * _ZDensity * _YDensity) + (y * _ZDensity) + z)].WorldPosition, gridCubeSize);
                Gizmos.DrawWireCube(fireVoxel.WorldPosition, gridCubeSize);
            }
        }
#endif
    }
}
