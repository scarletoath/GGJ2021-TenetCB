using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tenet
{
    public class DestroyAfterParticlesDied : MonoBehaviour
    {
		ParticleSystem system = null;

        // Start is called before the first frame update
        void Start()
        {
			system = GetComponent<ParticleSystem>();
        }

        // Update is called once per frame
        void Update()
        {
			if( system.isStopped)
			{
				if( system.particleCount <= 0)
				{
					Destroy(gameObject);
				}
			}
        }
    }
}
