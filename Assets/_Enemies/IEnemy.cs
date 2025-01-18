using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemy
{
    void Damage(GameObject _gameObject, int damageAmount, int critDamage, int armourDamage);
}
