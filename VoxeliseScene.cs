using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine.VFX;

namespace GRUProject.Firepropagation
{
    public class VoxeliseScene : MonoBehaviour
    {
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
        public FireVoxel[] _fireVoxels;
        [SerializeField, HideInInspector]
        private FireVoxel[] _VoxelMap;

        [SerializeField]
        private VisualEffect _VisualEffect;
        [SerializeField]
        private float _LightningSpeed = 0.1f;
        private const string _graphicsBufferName = "PositionsList";
        public List<FireVoxel> BurningVoxels => _burningVoxels;
        private List<FireVoxel> _burningVoxels = new List<FireVoxel>();
        private Voxeliser _voxeliser;
        Vector3[] _burningVoxelsPositions;

        public ComputeShader voxelComputeShader;
        private int voxelKernel;

        ComputeBuffer voxelBuffer;
        ComputeBuffer burningVoxelPositionsBuffer;
        private int bufferSize = 0;
        private GraphicsBuffer m_positionBuffer;
        struct VoxelData
        {
            int IsAble;
            int Ignited;
            int Burning;
            float LightningValue;
            float3 WorldPosition;
            int3 Index;
        };
        private void Awake()
        {
            _fireVoxels = _VoxelMap.ToArray();
            _burningVoxelsPositions = new Vector3[_fireVoxels.Length];

            for (int i = 0; i < _fireVoxels.Length; i++)
            {
                _burningVoxelsPositions[i] = Vector3.zero;
            }
            for (int i = 0; i < _fireVoxels.Length; i++)
            {
                _fireVoxels[i].Ignited = 0;
                _fireVoxels[i].Burning = 0;
            }
            m_positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Append, _fireVoxels.Length, 12);
            m_positionBuffer.SetData(_burningVoxelsPositions);
            if (_VisualEffect.HasGraphicsBuffer(_graphicsBufferName))
                _VisualEffect.SetGraphicsBuffer(_graphicsBufferName, m_positionBuffer);



            voxelKernel = voxelComputeShader.FindKernel("CSTick");

            voxelBuffer = new ComputeBuffer(_fireVoxels.Length, Marshal.SizeOf(typeof(VoxelData)));
            burningVoxelPositionsBuffer = new ComputeBuffer(_fireVoxels.Length, 3 * sizeof(float));

            voxelBuffer.SetData(_fireVoxels);

            voxelComputeShader.SetVector("IgnitionPos", Vector3.up * 5000);
            voxelComputeShader.SetBuffer(voxelKernel, "VoxelBuffer", voxelBuffer);
            voxelComputeShader.SetBuffer(voxelKernel, "BurningVoxelPositionsBuffer", m_positionBuffer);
            voxelComputeShader.SetFloat("LightningSpeed", _LightningSpeed * Time.deltaTime);
            voxelComputeShader.SetInt("VoxelCount", _fireVoxels.Length);
            voxelComputeShader.SetInt("XDensity", _XDensity);
            voxelComputeShader.SetInt("YDensity", _YDensity);
            voxelComputeShader.SetInt("ZDensity", _ZDensity);
            //InvokeRepeating(nameof(FireVoxelTick), 0, 0.5f);
            // var counter = 0;
            // for (int i = 0; i < _burningVoxels.Count; i++) //todo: move this to firevoxel tick when you add float with burning status
            // {
            //     _burningVoxelsPositions.Add(_burningVoxels[i].WorldPosition + (Random.insideUnitSphere * 0.05f));
            //     counter++;
            //     if (counter <= 200) continue;
            //     counter = 0;
            //     yield return null;
            // }
            // if (_burningVoxelsPositions.Count != 0)
            // {
            //     //  Debug.Log("try to send graphics buffer");
            //     if (m_positionBuffer != null)
            //         m_positionBuffer.Release();
            //     _VisualEffect.SetGraphicsBuffer(_graphicsBufferName, m_positionBuffer);
            // }
            int threadGroups = Mathf.CeilToInt(_fireVoxels.Length / 8.0f);
            voxelComputeShader.Dispatch(voxelKernel, threadGroups, 1, 1);
            //  StartCoroutine(EmitParticles());
        }
        private void Update()
        {
            int threadGroups = Mathf.CeilToInt(_fireVoxels.Length / 8.0f);
            voxelComputeShader.SetFloat("LightningSpeed", _LightningSpeed * Time.deltaTime);
            voxelComputeShader.Dispatch(voxelKernel, threadGroups, 1, 1);

            // voxelBuffer.GetData(_fireVoxels);
            //  burningVoxelPositionsBuffer.GetData(_burningVoxelsPositions);
            // m_positionBuffer.SetData(_burningVoxelsPositions);
        }
        [ContextMenu("Run and create voxels")]
        private void RunAndCreate()
        {
            Debug.Log("run");
            Run();
            _VoxelMap = new FireVoxel[_XDensity * _YDensity * _ZDensity];
            int voxelMapCounter = 0;
            for (int x = 0; x < _XDensity; x++)
            {
                for (int y = 0; y < _YDensity; y++)
                {
                    for (int z = 0; z < _ZDensity; z++)
                    {
                        _VoxelMap[voxelMapCounter] = (_voxeliser.VoxelMap[x][y][z]);
                        voxelMapCounter++;
                    }
                }
            }
        }

        // private IEnumerator EmitParticles()
        // {
        //     while (enabled)
        //     {
        //         var counter = 0;
        //         _burningVoxelsPositions.Clear();
        //         for (int i = 0; i < _burningVoxels.Count; i++) //todo: move this to firevoxel tick when you add float with burning status
        //         {
        //             _burningVoxelsPositions.Add(_burningVoxels[i].WorldPosition + (Random.insideUnitSphere * 0.05f));
        //             counter++;
        //             if (counter <= 200) continue;
        //             counter = 0;
        //             yield return null;
        //         }
        //         if (_burningVoxelsPositions.Count != 0)
        //         {
        //             //  Debug.Log("try to send graphics buffer");
        //             if (m_positionBuffer != null)
        //                 m_positionBuffer.Release();
        //             m_positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _burningVoxelsPositions.Count, 12);
        //             _VisualEffect.SetGraphicsBuffer(_graphicsBufferName, m_positionBuffer);
        //             m_positionBuffer.SetData(_burningVoxelsPositions);
        //         }
        //         yield return new WaitForSeconds(0.2f);
        //     }
        // }
        private void FireVoxelTick()
        {
            for (int i = 0; i < _fireVoxels.Length; i++)
            {
                var fireVoxel = _fireVoxels[i];
                if (fireVoxel.Ignited == 1 && fireVoxel.Burning != 1)
                {
                    if (fireVoxel.LightningValue >= 1)
                    {
                        fireVoxel.Burning = 1;
                        _burningVoxels.Add(fireVoxel);
                        _burningVoxelsPositions[i] = fireVoxel.WorldPosition;
                        m_positionBuffer.SetData(_burningVoxelsPositions);
                    }
                    else
                    {
                        fireVoxel.LightningValue += _LightningSpeed;
                    }
                }
                _fireVoxels[i] = fireVoxel;
            }
            foreach (var t in _fireVoxels.Where(t => t.Burning == 1))
            {
                IgniteNeighbours(t);
            }

        }
        void OnDestroy()
        {
            voxelBuffer.Release();
            burningVoxelPositionsBuffer.Release();
            m_positionBuffer.Release();
        }
        private void IgniteNeighbours(FireVoxel fireVoxel)
        {
            //x axis
            if (fireVoxel.Index.x >= 0 && fireVoxel.Index.x < _XDensity - 1)
            {
                var neighbour = _VoxelMap[((fireVoxel.Index.x + 1) * _ZDensity * _YDensity) + (fireVoxel.Index.y * _ZDensity) + (fireVoxel.Index.z)];
                if (neighbour.IsAble == 1)
                {
                    neighbour.Ignited = 1;
                }
            }
            if (fireVoxel.Index.x > 0 && fireVoxel.Index.x <= _XDensity - 1)
            {
                var neighbour = _VoxelMap[((fireVoxel.Index.x - 1) * _ZDensity * _YDensity) + (fireVoxel.Index.y * _ZDensity) + (fireVoxel.Index.z)];
                if (neighbour.IsAble == 1)
                {
                    neighbour.Ignited = 1;
                }
            }

            //y axis
            if (fireVoxel.Index.y >= 0 && fireVoxel.Index.y < _YDensity - 1)
            {
                var neighbour = _VoxelMap[(fireVoxel.Index.x * _ZDensity * _YDensity) + ((fireVoxel.Index.y + 1) * _ZDensity) + (fireVoxel.Index.z)];
                if (neighbour.IsAble == 1)
                {
                    neighbour.Ignited = 1;
                }

            }
            if (fireVoxel.Index.y > 0 && fireVoxel.Index.y <= _YDensity - 1)
            {
                var neighbour = _VoxelMap[(fireVoxel.Index.x * _ZDensity * _YDensity) + ((fireVoxel.Index.y - 1) * _ZDensity) + (fireVoxel.Index.z)];
                if (neighbour.IsAble == 1)
                {
                    neighbour.Ignited = 1;
                }
            }

            //z axis
            if (fireVoxel.Index.z >= 0 && fireVoxel.Index.z < _ZDensity - 1)
            {
                var neighbour = _VoxelMap[(fireVoxel.Index.x * _ZDensity * _YDensity) + (fireVoxel.Index.y * _ZDensity) + (fireVoxel.Index.z + 1)];
                if (neighbour.IsAble == 1)
                {
                    neighbour.Ignited = 1;
                }
            }
            if (fireVoxel.Index.z > 0 && fireVoxel.Index.z <= _ZDensity - 1)
            {
                var neighbour = _VoxelMap[(fireVoxel.Index.x * _ZDensity * _YDensity) + (fireVoxel.Index.y * _ZDensity) + (fireVoxel.Index.z - 1)];
                if (neighbour.IsAble == 1)
                {
                    neighbour.Ignited = 1;
                }
            }
        }

        public bool TryToIgnitePoint(Vector3 point)
        {
            // var pointBound = new Bounds(point, Vector3.zero);
            // if (!_Bounds.Intersects(pointBound)) return false;
            // var index = GetCellIndexAtPosition(point);
            // if (_VoxelMap[index].IsAble != 1) return false;
            // _VoxelMap[index].Ignited = 1;
            // _fireVoxels = _VoxelMap.Where(x => x.IsAble == 1).ToArray();
            voxelComputeShader.SetVector("IgnitionPos", point);
            return true;
        }
        // public void TryToIgniteArea(Vector3 position, float radius)
        // {
        //     var fireVoxels = GetFireVoxelsInArea(position, radius);
        //     foreach (var fireVoxel in fireVoxels)
        //     {
        //         fireVoxel.Ignited = true;
        //     }
        // }
        // public void TryToExtinguish(Vector3 position, float radius)
        // {
        //     var fireVoxels = GetFireVoxelsInArea(position, radius);
        //     foreach (var fireVoxel in fireVoxels)
        //     {
        //         if (!fireVoxel.Ignited && !fireVoxel.Burning) continue;
        //         fireVoxel.Ignited = false;
        //         fireVoxel.Burning = false;
        //         if (_burningVoxels.Contains(fireVoxel))
        //         {
        //             _burningVoxels.Remove(fireVoxel);
        //         }
        //     }
        // }
        private int GetCellIndexAtPosition(Vector3 position)
        {
            var gridCubeSize = new Vector3(
                _Bounds.size.x / _XDensity,
                _Bounds.size.y / _YDensity,
                _Bounds.size.z / _ZDensity);
            var worldCentre = _Bounds.min + gridCubeSize / 2;
            var pointNormalization = position - worldCentre;
            var x = (int)(pointNormalization.x / (gridCubeSize.x));
            var y = (int)(pointNormalization.y / (gridCubeSize.y));
            var z = (int)(pointNormalization.z / (gridCubeSize.z));
            return ((x * _ZDensity * _YDensity) + (y * _ZDensity) + z);
        }
        private List<FireVoxel> GetFireVoxelsInArea(Vector3 position, float radius)
        {
            var gridCubeSize = new Vector3(
                _Bounds.size.x / _XDensity,
                _Bounds.size.y / _YDensity,
                _Bounds.size.z / _ZDensity);
            var listOfVoxels = new List<FireVoxel>();
            foreach (var t in _fireVoxels) //fix this in the future
            {
                if (Vector3.Distance(position, t.WorldPosition) < radius)
                {
                    listOfVoxels.Add(t);
                }
            }
            return listOfVoxels;
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


            // foreach (var fireVoxel in _VoxelMap.Where(fireVoxel => fireVoxel.IsAble == 1))
            // {
            //     //       Gizmos.DrawWireCube(_VoxelMap[((x * _ZDensity * _YDensity) + (y * _ZDensity) + z)].WorldPosition, gridCubeSize);
            //     Gizmos.DrawWireCube(fireVoxel.WorldPosition, gridCubeSize);
            // }
        }
#endif
    }
}