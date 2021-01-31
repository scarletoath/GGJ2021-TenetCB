using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tenet
{
    public class DestroyAfterXSeconds : MonoBehaviour
    {
		[SerializeField] float lifetime;
        // Start is called before the first frame update
        void Start()
		{
			StartCoroutine( SelfDestruct() );
		}

        // Update is called once per frame
        void Update()
        {
        
        }

		IEnumerator SelfDestruct()
		{
			yield return new WaitForSeconds( lifetime );
			Destroy( gameObject );
		}
	}
}
