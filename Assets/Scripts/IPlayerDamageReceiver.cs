using UnityEngine;

// Not used anymore, but kept for reference
public interface IPlayerDamageReceiver
{
    bool IsPowered { get; }
    void LosePowerUp();
    void Die();
}