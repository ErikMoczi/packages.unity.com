namespace UnityEngine.Experimental.U2D.Animation
{
    [ExecuteInEditMode]
    public class SpriteIK : MonoBehaviour
    {
        public Transform bone;
        public int depth = 2;

        Transform tr;

        void Start()
        {
            tr = this.transform;
        }

        void DoIK(Transform current, Vector3 from, Vector3 to, int d)
        {
            var constraint = current.GetComponent<SpriteIKConstraint>();
            var q = Quaternion.FromToRotation(from.normalized, to.normalized);

            if (constraint != null)
            {
                //var r = current.rotation * q;
                current.rotation *= q;

                var z = current.localRotation.eulerAngles.z;

                // remap this to 180, -180
                if (z > 180.0f)
                    z -= 360.0f;

                if (z > constraint.max || z < constraint.min)
                {
                    z = Mathf.Clamp(z, constraint.min, constraint.max);
                    current.localRotation = Quaternion.Euler(0, 0, z);
                }
            }
            else
            {
                current.rotation *= q;
            }

            if (d < depth)
            {
                var p = current.parent;
                if (p != null)
                {
                    DoIK(p, bone.position - p.position, tr.position - p.position, d + 1);
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (bone == null)
                return;

            var r = bone.rotation;

            // just tack on a random rotaton to lowerarm
            {
                // prerotate
                var toNextBone = bone.parent.position - bone.parent.parent.position;
                var toHandBone = tr.position - bone.parent.parent.position;

                Quaternion q = Quaternion.FromToRotation(toNextBone.normalized, toHandBone.normalized);
                var hint = q.eulerAngles.z < 180.0f ? -10.0f : 10.0f;

                var z = bone.parent.parent.localRotation.eulerAngles.z;
                z += hint;
                bone.parent.parent.localRotation = Quaternion.Euler(0, 0, z);
            }

            var delta = 1.0f;
            while (delta > 0.01f)
            {
                var mag1 = (tr.position - bone.position).sqrMagnitude;

                var p = bone.parent;
                DoIK(p, bone.position - p.position, tr.position - p.position, 1);

                var mag2 = (tr.position - bone.position).sqrMagnitude;
                delta = mag1 - mag2;
            }

            bone.rotation = r;
        }
    }
}
