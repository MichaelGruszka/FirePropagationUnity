using UnityEngine;
namespace GRUProject.Firepropagation
{

    public class Lighter : MonoBehaviour
    {
        [SerializeField]
        private VoxeliseScene _VoxeliseScene;
        void LateUpdate()
        {
            if (_VoxeliseScene.TryToIgnitePoint(transform.position))
            {
                Destroy(this);
            }
        }
    }
}
