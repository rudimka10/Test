using System.Collections.Generic;
using UnityEngine;

namespace InfectionShot
{
    /// <summary>Lists the authored obstacles so nothing has to search the scene.</summary>
    public sealed class FieldView : MonoBehaviour
    {
        [SerializeField] private ObstacleView[] obstacles = new ObstacleView[0];

        public List<ObstacleModel> BuildModels(float radius)
        {
            var models = new List<ObstacleModel>(obstacles.Length);
            for (int i = 0; i < obstacles.Length; i++)
            {
                ObstacleView v = obstacles[i];
                Vector3 p = v.transform.position;
                models.Add(new ObstacleModel(i, new Vector3(p.x, 0f, p.z), radius, v.Group, v.Blocking));
            }

            return models;
        }

        public void Infect(int id, float delay, FxView fx)
        {
            obstacles[id].Infect(delay, fx);
        }

        public void RestoreAll()
        {
            for (int i = 0; i < obstacles.Length; i++) obstacles[i].Restore();
        }
    }
}
