using System.Collections.Generic;
using UnityEngine;

namespace Blockbound.WorldFeatures
{
    public class CloudLayerRenderer : MonoBehaviour
    {
        [SerializeField] private Material cloudMaterial;
        [SerializeField] private int cloudCount = 24;
        [SerializeField] private Vector2 worldSize = new Vector2(1200f, 1200f);
        [SerializeField] private float height = 210f;
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private Vector2 sizeRange = new Vector2(20f, 60f);

        private readonly List<Transform> cloudTransforms = new List<Transform>();

        private void Start()
        {
            if (cloudMaterial == null)
                return;

            for (int i = 0; i < cloudCount; i++)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "VisualCloud_" + i;
                go.transform.SetParent(transform, false);
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                float x = Random.Range(-worldSize.x * 0.5f, worldSize.x * 0.5f);
                float z = Random.Range(-worldSize.y * 0.5f, worldSize.y * 0.5f);
                float sx = Random.Range(sizeRange.x, sizeRange.y);
                float sz = Random.Range(sizeRange.x * 0.6f, sizeRange.y * 0.6f);

                go.transform.position = new Vector3(x, height + Random.Range(-8f, 8f), z);
                go.transform.localScale = new Vector3(sx, sz, 1f);

                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = cloudMaterial;

                Collider c = go.GetComponent<Collider>();
                if (c != null)
                    Destroy(c);

                cloudTransforms.Add(go.transform);
            }
        }

        private void Update()
        {
            float dx = moveSpeed * Time.deltaTime;

            for (int i = 0; i < cloudTransforms.Count; i++)
            {
                Transform t = cloudTransforms[i];
                Vector3 p = t.position;
                p.x += dx;

                if (p.x > worldSize.x * 0.5f)
                    p.x = -worldSize.x * 0.5f;

                t.position = p;
            }
        }
    }
}