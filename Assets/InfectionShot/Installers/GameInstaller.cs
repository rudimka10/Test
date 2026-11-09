using UnityEngine;
using Zenject;

namespace InfectionShot
{
    /// <summary>Composition root: the only place that wires models, controllers and views together.</summary>
    public sealed class GameInstaller : MonoInstaller
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Transform routeStart;
        [SerializeField] private Transform door;
        [SerializeField] private FieldView fieldView;
        [SerializeField] private PlayerView playerView;
        [SerializeField] private ShotView shotView;
        [SerializeField] private LaneView laneView;
        [SerializeField] private DoorView doorView;
        [SerializeField] private FxView fxView;
        [SerializeField] private HudView hudView;
        [SerializeField] private PointerInputView pointerView;

        public override void InstallBindings()
        {
            Container.BindInstance(config);
            Container.BindInstance(fieldView);
            Container.BindInstance(playerView);
            Container.BindInstance(shotView);
            Container.BindInstance(laneView);
            Container.BindInstance(doorView);
            Container.BindInstance(fxView);
            Container.BindInstance(hudView);
            Container.BindInstance(pointerView);

            // Models
            Container.BindInstance(new PathModel(routeStart.position, door.position));
            Container.BindInstance(new FieldModel(fieldView.BuildModels(config.obstacleRadius)));
            Container.Bind<GameModel>().AsSingle();
            Container.Bind<DoorModel>().AsSingle();

            // Controllers
            Container.BindInterfacesAndSelfTo<GameController>().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerController>().AsSingle();
            Container.BindInterfacesAndSelfTo<ShotController>().AsSingle();
            Container.BindInterfacesAndSelfTo<ChargeController>().AsSingle();
            Container.BindInterfacesAndSelfTo<PathController>().AsSingle();
            Container.BindInterfacesAndSelfTo<HudController>().AsSingle();
        }
    }
}
