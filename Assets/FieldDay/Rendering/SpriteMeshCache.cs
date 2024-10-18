using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.Rendering {
    public class SpriteMeshCache<TVertex> where TVertex : unmanaged {
        private readonly Dictionary<int, Mesh> m_MeshMap;
        private readonly MeshData16<TVertex> m_Builder;
        private GenerateMeshFromSprite<TVertex> m_BuildFunc;

        public SpriteMeshCache(GenerateMeshFromSprite<TVertex> buildFunc) {
            Assert.NotNull(buildFunc);
            m_BuildFunc = buildFunc;
            m_MeshMap = new Dictionary<int, Mesh>(16);
            m_Builder = new MeshData16<TVertex>(64);
        }

        public Mesh GetMesh(Sprite sprite) {
            Assert.NotNull(sprite);

            int id = sprite.GetInstanceID();
            if (m_MeshMap.TryGetValue(id, out Mesh mesh)) {
                return mesh;
            }

            m_Builder.Clear();
            m_BuildFunc(new SpriteMeshInfo(sprite), m_Builder);
            Mesh m = new Mesh();
            m_Builder.Upload(m);
            m.UploadMeshData(true);
            m_Builder.Clear();

            m_MeshMap.Add(id, m);
            return m;
        }
    }

    public delegate void GenerateMeshFromSprite<TVertex>(SpriteMeshInfo info, MeshData16<TVertex> builder) where TVertex : unmanaged;
}