using UnityEngine;
using Fusion;

public class BulletMovement : NetworkBehaviour
{
    [SerializeField] private int speed;
    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            transform.Translate(speed * Runner.DeltaTime, 0, 0);

            if (transform.position.x > 10)
            {
                Runner.Despawn(Object);
            }
        }
    }
}
