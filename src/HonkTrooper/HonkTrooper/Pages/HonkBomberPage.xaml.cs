﻿using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Display;

namespace HonkTrooper
{
    public sealed partial class HonkBomberPage : Page
    {
        #region Fields

        private PlayerBalloon _player;
        private readonly Random _random;

        private readonly Scene _scene_game;
        private readonly Scene _scene_main_menu;
        private readonly Controller _game_controller;

        private readonly HealthBar _player_health_bar;
        private readonly HealthBar _ufo_boss_health_bar;
        private readonly HealthBar _vehicle_boss_health_bar;
        private readonly HealthBar _powerUp_health_bar;
        private readonly HealthBar _sound_pollution_health_bar;

        private readonly ScoreBar _game_score_bar;
        private readonly StackPanel _health_bars;

        private readonly Threashold _ufo_boss_threashold;
        private readonly Threashold _vehicle_boss_threashold;
        private readonly Threashold _enemy_threashold;

        private readonly double _sound_pollution_max_limit = 6; // max 3 vehicles or ufos honking to trigger sound pollution limit

        private readonly double _vehicle_boss_threashold_limit = 25; // first vehicle Boss will appear
        private readonly double _vehicle_boss_threashold_limit_increase = 15;

        //TODO: set defaults _UFO_BOSS_threashold_limit = 50
        private readonly double _ufo_boss_threashold_limit = 50; // first UfoBoss will appear
        private readonly double _ufo_boss_threashold_limit_increase = 15;

        //TODO: set defaults _enemy_threashold_limit = 80
        private readonly double _enemy_threashold_limit = 80; // after first enemies will appear
        private readonly double _enemy_threashold_limit_increase = 10;

        private double _enemy_kill_count;
        private readonly double _enemy_kill_count_limit = 20;

        private bool _enemy_fleet_appeared;

        private readonly AudioStub _audio_stub;

        private int _selected_player_template;

        #endregion

        #region Ctor

        public HonkBomberPage()
        {
            this.InitializeComponent();

            _scene_game = this.GameScene;
            _scene_main_menu = this.MainMenuScene;
            _player_health_bar = this.PlayerHealthBar;
            _ufo_boss_health_bar = this.UfoBossHealthBar;
            _vehicle_boss_health_bar = this.VehicleBossHealthBar;
            _powerUp_health_bar = this.PowerUpHealthBar;
            _sound_pollution_health_bar = this.SoundPollutionBar;

            _game_controller = this.GameController;
            _game_score_bar = this.GameScoreBar;
            _health_bars = this.HealthBars;

            _ufo_boss_threashold = new Threashold(_ufo_boss_threashold_limit);
            _vehicle_boss_threashold = new Threashold(_vehicle_boss_threashold_limit);
            _enemy_threashold = new Threashold(_enemy_threashold_limit);

            ToggleHudVisibility(Visibility.Collapsed);

            _random = new Random();

            _audio_stub = new AudioStub(
                (SoundType.GAME_BACKGROUND_MUSIC, 0.5, true),
                (SoundType.UFO_BOSS_BACKGROUND_MUSIC, 0.5, true),
                (SoundType.AMBIENCE, 0.6, true),
                (SoundType.GAME_START, 1, false),
                (SoundType.GAME_PAUSE, 1, false),
                (SoundType.GAME_OVER, 1, false),
                (SoundType.UFO_ENEMY_ENTRY, 1, false));

            ScreenExtensions.Width = Constants.DEFAULT_SCENE_WIDTH;
            ScreenExtensions.Height = Constants.DEFAULT_SCENE_HEIGHT;

            _scene_main_menu.SetRenderTransformOrigin(0.5);
            SetScreenScaling();

            Loaded += HonkBomberPage_Loaded;
            Unloaded += HonkBomberPage_Unloaded;
        }

        #endregion

        #region Methods

        #region Game

        private bool PauseGame()
        {
            _audio_stub.Play(SoundType.GAME_PAUSE);

            _audio_stub.Pause(SoundType.AMBIENCE);

            if (UfoBossExists())
            {
                //_audio_stub.Pause(SoundType.UFO_BOSS_BACKGROUND_MUSIC);
            }
            else
            {
                _audio_stub.Pause(SoundType.GAME_BACKGROUND_MUSIC);
            }

            ToggleHudVisibility(Visibility.Collapsed);

            _scene_game.Pause();
            _scene_main_menu.Play();

            _game_controller.DeactivateGyrometerReading();
            _game_controller.SetDefaultThumbstickPosition();

            GenerateTitleScreen("Game Paused");

            return true;
        }

        private void ResumeGame()
        {
            _audio_stub.Resume(SoundType.AMBIENCE);

            if (UfoBossExists())
            {
                //_audio_stub.Resume(SoundType.UFO_BOSS_BACKGROUND_MUSIC);
            }
            else
            {
                _audio_stub.Resume(SoundType.GAME_BACKGROUND_MUSIC);
            }

            ToggleHudVisibility(Visibility.Visible);

            _scene_game.Play();
            _scene_main_menu.Pause();

            _game_controller.ActivateGyrometerReading();
            _game_controller.FocusAttackButton();
        }

        private void NewGame()
        {
            LoggerExtensions.Log("New Game Started.");

            _audio_stub.Play(SoundType.AMBIENCE, SoundType.GAME_BACKGROUND_MUSIC);

            _game_controller.Reset();

            _game_score_bar.Reset();
            _powerUp_health_bar.Reset();

            _ufo_boss_health_bar.Reset();
            _vehicle_boss_health_bar.Reset();

            _sound_pollution_health_bar.Reset();
            _sound_pollution_health_bar.SetMaxiumHealth(_sound_pollution_max_limit);
            _sound_pollution_health_bar.SetIcon(Constants.CONSTRUCT_TEMPLATES.FirstOrDefault(x => x.ConstructType == ConstructType.HONK).Uri);
            _sound_pollution_health_bar.SetBarForegroundColor(color: Colors.Purple);

            _ufo_boss_threashold.Reset(_ufo_boss_threashold_limit);
            _vehicle_boss_threashold.Reset(_vehicle_boss_threashold_limit);

            _enemy_threashold.Reset(_enemy_threashold_limit);
            _enemy_kill_count = 0;
            _enemy_fleet_appeared = false;

            GeneratePlayerBalloon();
            RecycleLogicalConstructs();

            _scene_game.SceneState = SceneState.GAME_RUNNING;
            _scene_game.Play();

            _scene_main_menu.Pause();

            ToggleHudVisibility(Visibility.Visible);

            _game_controller.FocusAttackButton();
            _game_controller.SetDefaultThumbstickPosition();
            _game_controller.ActivateGyrometerReading();
        }

        private void GameOver()
        {
            // if player is dead game keeps playing in the background but scene state goes to game over
            if (_player.IsDead)
            {
                _audio_stub.Stop(SoundType.AMBIENCE, SoundType.GAME_BACKGROUND_MUSIC, SoundType.UFO_BOSS_BACKGROUND_MUSIC);

                if (_scene_game.Children.OfType<UfoBoss>().FirstOrDefault(x => x.IsAnimating) is UfoBoss ufoBoss)
                {
                    ufoBoss.SetWinStance();
                    ufoBoss.StopSoundLoop();
                }

                _audio_stub.Play(SoundType.GAME_OVER);

                _scene_main_menu.Play();
                _scene_game.SceneState = SceneState.GAME_STOPPED;

                ToggleHudVisibility(Visibility.Collapsed);
                GenerateTitleScreen("Game Over");

                _game_controller.DeactivateGyrometerReading();
            }
        }

        private void RecycleLogicalConstructs()
        {
            foreach (var construct in _scene_game.Children.OfType<Construct>()
                .Where(x => x.ConstructType is
                ConstructType.VEHICLE_LARGE or
                ConstructType.VEHICLE_SMALL or
                ConstructType.VEHICLE_BOSS or
                ConstructType.UFO_BOSS or
                ConstructType.HONK or
                ConstructType.PLAYER_ROCKET or
                ConstructType.PLAYER_ROCKET_SEEKING or
                ConstructType.PLAYER_FIRE_CRACKER or
                ConstructType.UFO_BOSS_ROCKET or
                ConstructType.UFO_BOSS_ROCKET_SEEKING or
                ConstructType.UFO_ENEMY or
                ConstructType.UFO_ENEMY_ROCKET or
                ConstructType.VEHICLE_BOSS_ROCKET or
                ConstructType.POWERUP_PICKUP or
                ConstructType.HEALTH_PICKUP))
            {
                construct.IsAnimating = false;

                construct.SetPosition(
                     left: -3000,
                     top: -3000);

                if (construct is UfoBoss ufoBoss)
                {
                    ufoBoss.IsAttacking = false;
                    ufoBoss.Health = 0;
                }

                if (construct is VehicleBoss vehicleboss)
                {
                    vehicleboss.IsAttacking = false;
                    vehicleboss.Health = 0;
                }
            }
        }

        private void RepositionHoveringTitleScreens()
        {
            foreach (var screen in _scene_main_menu.Children.OfType<HoveringTitleScreen>().Where(x => x.IsAnimating))
            {
                screen.Reposition();
            }
        }

        #endregion

        #region DisplayOrientationChangeScreen

        private bool SpawnDisplayOrientationChangeScreen()
        {
            DisplayOrientationChangeScreen displayOrientationChangeScreen = null;

            displayOrientationChangeScreen = new(
                animateAction: AnimateDisplayOrientationChangeScreen,
                recycleAction: (se) => { return true; });

            displayOrientationChangeScreen.SetPosition(
                left: -3000,
                top: -3000);

            _scene_main_menu.AddToScene(displayOrientationChangeScreen);

            return true;
        }

        private bool GenerateDisplayOrientationChangeScreen()
        {
            if (_scene_main_menu.Children.OfType<DisplayOrientationChangeScreen>().FirstOrDefault(x => x.IsAnimating == false) is DisplayOrientationChangeScreen displayOrientationChangeScreen)
            {
                displayOrientationChangeScreen.IsAnimating = true;
                displayOrientationChangeScreen.Reposition();

                return true;
            }

            return false;
        }

        private bool AnimateDisplayOrientationChangeScreen(Construct displayOrientationChangeScreen)
        {
            DisplayOrientationChangeScreen screen1 = displayOrientationChangeScreen as DisplayOrientationChangeScreen;
            screen1.Hover();
            return true;
        }

        private void RecycleDisplayOrientationChangeScreen(DisplayOrientationChangeScreen displayOrientationChangeScreen)
        {
            displayOrientationChangeScreen.IsAnimating = false;
            displayOrientationChangeScreen.SetPosition(left: -3000, top: -3000);

            LoggerExtensions.Log("Screen Orientation Change Promt Recyled.");
        }

        #endregion

        #region TitleScreen

        private bool SpawnTitleScreen()
        {
            TitleScreen titleScreen = null;

            titleScreen = new(
                animateAction: AnimateTitleScreen,
                recycleAction: (se) => { return true; },
                playAction: () =>
                {
                    if (_scene_game.SceneState == SceneState.GAME_STOPPED)
                    {
                        if (ScreenExtensions.RequiredScreenOrientation == ScreenExtensions.GetScreenOrienation())
                        {
                            RecycleTitleScreen(titleScreen);
                            GeneratePlayerSelectionScreen();
                            ScreenExtensions.EnterFullScreen(true);
                        }
                        else
                        {
                            ScreenExtensions.SetScreenOrientation(ScreenExtensions.RequiredScreenOrientation);
                        }
                    }
                    else
                    {
                        if (!_scene_game.IsAnimating)
                        {
                            if (ScreenExtensions.RequiredScreenOrientation == ScreenExtensions.GetScreenOrienation())
                            {
                                ResumeGame();
                                RecycleTitleScreen(titleScreen);
                            }
                            else
                            {
                                ScreenExtensions.SetScreenOrientation(ScreenExtensions.RequiredScreenOrientation);
                            }
                        }
                    }

                    return true;
                });

            titleScreen.SetPosition(
                left: -3000,
                top: -3000);

            _scene_main_menu.AddToScene(titleScreen);

            return true;
        }

        private bool GenerateTitleScreen(string title)
        {
            if (_scene_main_menu.Children.OfType<TitleScreen>().FirstOrDefault(x => x.IsAnimating == false) is TitleScreen titleScreen)
            {
                titleScreen.SetTitle(title);
                titleScreen.IsAnimating = true;
                titleScreen.Reposition();

                if (_player is not null)
                    titleScreen.SetContent(_player.GetContentUri());

                return true;
            }

            return false;
        }

        private bool AnimateTitleScreen(Construct titleScreen)
        {
            TitleScreen screen1 = titleScreen as TitleScreen;
            screen1.Hover();
            return true;
        }

        private void RecycleTitleScreen(TitleScreen titleScreen)
        {
            titleScreen.IsAnimating = false;
            titleScreen.SetPosition(left: -3000, top: -3000);
        }

        #endregion

        #region PlayerSelectionScreen

        private bool SpawnPlayerSelectionScreen()
        {
            PlayerSelectionScreen playerSelectionScreen = null;

            playerSelectionScreen = new(
                animateAction: AnimatePlayerSelectionScreen,
                recycleAction: (se) => { return true; },
                playAction: (int playerTemplate) =>
                {
                    _selected_player_template = playerTemplate;

                    if (_scene_game.SceneState == SceneState.GAME_STOPPED)
                    {
                        RecyclePlayerSelectionScreen(playerSelectionScreen);
                        NewGame();
                    }

                    return true;
                },
                backAction: () =>
                {
                    RecyclePlayerSelectionScreen(playerSelectionScreen);
                    GenerateTitleScreen("Honk Trooper");
                    return true;
                });

            playerSelectionScreen.SetPosition(
                left: -3000,
                top: -3000);

            _scene_main_menu.AddToScene(playerSelectionScreen);

            return true;
        }

        private bool GeneratePlayerSelectionScreen()
        {
            if (_scene_main_menu.Children.OfType<PlayerSelectionScreen>().FirstOrDefault(x => x.IsAnimating == false) is PlayerSelectionScreen playerSelectionScreen)
            {
                playerSelectionScreen.IsAnimating = true;
                playerSelectionScreen.Reposition();

                return true;
            }

            return false;
        }

        private bool AnimatePlayerSelectionScreen(Construct playerSelectionScreen)
        {
            PlayerSelectionScreen screen1 = playerSelectionScreen as PlayerSelectionScreen;
            screen1.Hover();
            return true;
        }

        private void RecyclePlayerSelectionScreen(PlayerSelectionScreen playerSelectionScreen)
        {
            playerSelectionScreen.IsAnimating = false;
            playerSelectionScreen.SetPosition(left: -3000, top: -3000);

            LoggerExtensions.Log("Player Selection Screen Recyled.");
        }

        #endregion

        #region InterimScreen

        private bool SpawnInterimScreen()
        {
            InterimScreen interimScreen = null;

            interimScreen = new(
                animateAction: AnimateInterimScreen,
                recycleAction: RecycleInterimScreen);

            interimScreen.SetPosition(
                left: -3000,
                top: -3000);

            _scene_main_menu.AddToScene(interimScreen);

            return true;
        }

        private bool GenerateInterimScreen(string title)
        {
            if (_scene_main_menu.Children.OfType<InterimScreen>().FirstOrDefault(x => x.IsAnimating == false) is InterimScreen interimScreen)
            {
                interimScreen.IsAnimating = true;
                interimScreen.SetTitle(title);
                interimScreen.Reposition();
                interimScreen.Reset();

                _scene_main_menu.Play();

                return true;
            }

            return false;
        }

        private bool AnimateInterimScreen(Construct interimScreen)
        {
            InterimScreen screen1 = interimScreen as InterimScreen;
            screen1.Hover();
            screen1.DepleteOnScreenDelay();
            return true;
        }

        private bool RecycleInterimScreen(Construct interimScreen)
        {
            if (interimScreen is InterimScreen interimScreen1 && interimScreen1.IsDepleted)
            {
                interimScreen.IsAnimating = false;
                interimScreen.SetPosition(left: -3000, top: -3000);

                _scene_main_menu.Pause();

                return true;
            }

            return false;
        }

        #endregion

        #region PlayerBalloon

        private bool SpawnPlayerBalloon()
        {
            var playerTemplate = _random.Next(1, 3);
            LoggerExtensions.Log($"Player Template: {playerTemplate}");

            _player = new(
                animateAction: AnimatePlayerBalloon,
                recycleAction: (_player) => { return true; });

            _player.SetPosition(
                  left: -3000,
                  top: -3000);

            SpawnDropShadow(source: _player);

            _scene_game.AddToScene(_player);

            return true;
        }

        private bool GeneratePlayerBalloon()
        {
            _player.IsAnimating = true;
            _player.Reset();
            _player.Reposition();
            _player.SetPlayerTemplate(_selected_player_template);

            SyncDropShadow(_player);
            SetPlayerHealthBar();

            return true;
        }

        private void SetPlayerHealthBar()
        {
            _player_health_bar.SetMaxiumHealth(_player.Health);
            _player_health_bar.SetValue(_player.Health);

            _player_health_bar.SetIcon(Constants.CONSTRUCT_TEMPLATES.FirstOrDefault(x => x.ConstructType == ConstructType.HEALTH_PICKUP).Uri);
            _player_health_bar.SetBarForegroundColor(color: Colors.Purple);
        }

        private bool AnimatePlayerBalloon(Construct player)
        {
            _player.Pop();
            _player.Hover();
            _player.DepleteAttackStance();
            _player.DepleteWinStance();
            _player.DepleteHitStance();

            if (_scene_game.SceneState == SceneState.GAME_RUNNING)
            {
                var count = _scene_game.Children.OfType<Vehicle>().Count(x => x.IsAnimating && x.WillHonk) + _scene_game.Children.OfType<UfoEnemy>().Count(x => x.IsAnimating && x.WillHonk);
                _sound_pollution_health_bar.SetValue(count * 2);

                if (_sound_pollution_health_bar.GetValue() >= _sound_pollution_health_bar.GetMaxiumHealth()) // loose score slowly if sound pollution has reached the limit
                {
                    _game_score_bar.LooseScore(0.01);
                }

                var scaling = ScreenExtensions.GetScreenSpaceScaling();
                var speed = (_scene_game.Speed + _player.SpeedOffset);

                _player.Move(
                    speed: speed,
                    sceneWidth: Constants.DEFAULT_SCENE_WIDTH * scaling,
                    sceneHeight: Constants.DEFAULT_SCENE_HEIGHT * scaling,
                    controller: _game_controller);

                ProcessPlayerAttack();
            }

            return true;
        }

        private void ProcessPlayerAttack()
        {
            if (_game_controller.IsAttacking)
            {
                if (UfoEnemyExists() || UfoBossExists())
                {
                    if (_powerUp_health_bar.HasHealth && (PowerUpType)_powerUp_health_bar.Tag == PowerUpType.SEEKING_BALLS)
                        GeneratePlayerRocketSeeking();
                    else
                        GeneratePlayerRocket();
                }
                else
                {
                    GeneratePlayerFireCracker();
                }

                _game_controller.IsAttacking = false;
            }
        }

        private void LoosePlayerHealth()
        {
            _player.SetPopping();

            if (_powerUp_health_bar.HasHealth && (PowerUpType)_powerUp_health_bar.Tag == PowerUpType.FORCE_SHIELD)
            {
                DepletePowerUp();
            }
            else
            {
                _player.LooseHealth();
                _player.SetHitStance();

                _player_health_bar.SetValue(_player.Health);

                if (_scene_game.Children.OfType<UfoBoss>().FirstOrDefault(x => x.IsAnimating && x.IsAttacking) is UfoBoss UfoBoss)
                    UfoBoss.SetWinStance();

                GameOver();
            }
        }

        #endregion

        #region Vehicle

        private bool SpawnVehicles()
        {
            for (int i = 0; i < 8; i++)
            {
                Vehicle vehicle = new(
                    animateAction: AnimateVehicle,
                    recycleAction: RecycleVehicle);

                _scene_game.AddToScene(vehicle);

                vehicle.SetPosition(
                    left: -3000,
                    top: -3000);
            }

            return true;
        }

        private bool GenerateVehicle()
        {
            if (!UfoBossExists() && !VehicleBossExists() && _scene_game.Children.OfType<Vehicle>().FirstOrDefault(x => x.IsAnimating == false) is Vehicle vehicle)
            {
                vehicle.IsAnimating = true;
                vehicle.Reset();
                vehicle.Reposition();

                return true;
            }
            return false;
        }

        private bool AnimateVehicle(Construct vehicle)
        {
            Vehicle vehicle1 = vehicle as Vehicle;

            vehicle.Pop();

            var speed = (_scene_game.Speed + vehicle.SpeedOffset);

            MoveConstructBottomRight(construct: vehicle, speed: speed);

            if (_scene_game.SceneState == SceneState.GAME_RUNNING)
            {
                if (vehicle1.Honk())
                    GenerateVehicleHonk(vehicle1);
            }

            PreventVehicleOverlapping(vehicle);

            return true;
        }

        private bool RecycleVehicle(Construct vehicle)
        {
            var hitBox = vehicle.GetHitBox();

            if (hitBox.Top > Constants.DEFAULT_SCENE_HEIGHT || hitBox.Left > Constants.DEFAULT_SCENE_WIDTH)
            {
                vehicle.IsAnimating = false;

                vehicle.SetPosition(
                    left: -3000,
                    top: -3000);
            }

            return true;
        }

        private void PreventVehicleOverlapping(Construct vehicle)
        {
            var vehicle_distantHitBox = vehicle.GetDistantHitBox();

            if (_scene_game.Children.OfType<Vehicle>()
                .FirstOrDefault(x => x.IsAnimating && x.GetHorizontalHitBox().IntersectsWith(vehicle.GetHorizontalHitBox())) is Construct collidingVehicle)
            {
                var hitBox = vehicle.GetHitBox();

                if (vehicle.SpeedOffset == collidingVehicle.SpeedOffset)
                {
                    if (vehicle.SpeedOffset > -2)
                        vehicle.SpeedOffset--;
                }
                else
                {
                    if (vehicle.SpeedOffset > collidingVehicle.SpeedOffset) // vehicle is faster
                    {
                        vehicle.SpeedOffset = collidingVehicle.SpeedOffset;
                    }
                    else if (collidingVehicle.SpeedOffset > vehicle.SpeedOffset) // colliding vehicle is faster
                    {
                        collidingVehicle.SpeedOffset = vehicle.SpeedOffset;
                    }
                }
            }
        }

        #endregion        

        #region RoadMark

        private bool SpawnRoadMarks()
        {
            for (int i = 0; i < 10; i++)
            {
                RoadMark roadMark = new(
                    animateAction: AnimateRoadMark,
                    recycleAction: RecycleRoadMark);

                roadMark.SetPosition(
                    left: -3000,
                    top: -3000);

                _scene_game.AddToScene(roadMark);
            }

            return true;
        }

        private bool GenerateRoadMark()
        {
            if (_scene_game.Children.OfType<RoadMark>().FirstOrDefault(x => x.IsAnimating == false) is RoadMark roadMark)
            {
                roadMark.IsAnimating = true;

                roadMark.SetPosition(
                  left: roadMark.Height * -1,
                  top: roadMark.Height * -1,
                  z: 0);

                return true;
            }

            return false;
        }

        private bool AnimateRoadMark(Construct roadMark)
        {
            var speed = (_scene_game.Speed + roadMark.SpeedOffset);
            MoveConstructBottomRight(construct: roadMark, speed: speed);
            return true;
        }

        private bool RecycleRoadMark(Construct roadMark)
        {
            var hitBox = roadMark.GetHitBox();

            if (hitBox.Top > Constants.DEFAULT_SCENE_HEIGHT || hitBox.Left - roadMark.Height > Constants.DEFAULT_SCENE_WIDTH)
            {
                roadMark.IsAnimating = false;

                roadMark.SetPosition(
                    left: -3000,
                    top: -3000);
            }

            return true;
        }

        #endregion

        #region RoadSideWalkSlope

        private bool SpawnRoadSideWalkSlopes()
        {
            for (int i = 0; i < 10; i++)
            {
                RoadSideWalkSlope roadSideStripe = new(
                    animateAction: AnimateRoadSideWalkSlope,
                    recycleAction: RecycleRoadSideWalkSlope);

                roadSideStripe.SetPosition(
                    left: -3000,
                    top: -3000);

                _scene_game.AddToScene(roadSideStripe);
            }

            return true;
        }

        private bool GenerateRoadSideWalkSlopeTop()
        {
            if (_scene_game.Children.OfType<RoadSideWalkSlope>().FirstOrDefault(x => x.IsAnimating == false) is RoadSideWalkSlope roadSideStripe)
            {
                roadSideStripe.IsAnimating = true;

                roadSideStripe.SetPosition(
                    left: (Constants.DEFAULT_SCENE_WIDTH / 5.4),
                    top: (roadSideStripe.Height * -1) - 16.5,
                    z: 0);

                return true;
            }

            return false;
        }

        private bool GenerateRoadSideWalkSlopeBottom()
        {
            if (_scene_game.Children.OfType<RoadSideWalkSlope>().FirstOrDefault(x => x.IsAnimating == false) is RoadSideWalkSlope roadSideStripe)
            {
                roadSideStripe.IsAnimating = true;

                roadSideStripe.SetPosition(
                    left: (roadSideStripe.Height * -1),
                    top: (Constants.DEFAULT_SCENE_HEIGHT / 2.1) - 4.5,
                    z: 0);

                return true;
            }

            return false;
        }

        private bool AnimateRoadSideWalkSlope(Construct roadSideStripe)
        {
            var speed = (_scene_game.Speed + roadSideStripe.SpeedOffset);
            MoveConstructBottomRight(construct: roadSideStripe, speed: speed);
            return true;
        }

        private bool RecycleRoadSideWalkSlope(Construct roadSideStripe)
        {
            var hitBox = roadSideStripe.GetHitBox();

            if (hitBox.Top - roadSideStripe.Height > Constants.DEFAULT_SCENE_HEIGHT || hitBox.Left - roadSideStripe.Height > Constants.DEFAULT_SCENE_WIDTH)
            {
                roadSideStripe.IsAnimating = false;

                roadSideStripe.SetPosition(
                    left: -3000,
                    top: -3000);
            }

            return true;
        }

        #endregion

        #region RoadSideWalk

        private bool SpawnRoadSideWalks()
        {
            for (int i = 0; i < 7; i++)
            {
                RoadSideWalk radSidePatch = new(
                animateAction: AnimateRoadSideWalk,
                recycleAction: RecycleRoadSideWalk);

                radSidePatch.SetPosition(
                    left: -3000,
                    top: -3000);

                _scene_game.AddToScene(radSidePatch);
            }

            return true;
        }

        private bool GenerateRoadSideWalkTop()
        {
            if (_scene_game.Children.OfType<RoadSideWalk>().FirstOrDefault(x => x.IsAnimating == false) is RoadSideWalk roadSidePatch)
            {
                roadSidePatch.IsAnimating = true;

                roadSidePatch.SetPosition(
                    left: (Constants.DEFAULT_SCENE_WIDTH / 2.25 - roadSidePatch.Width),
                    top: roadSidePatch.Height * -1,
                    z: 0);

                return true;
            }

            return false;
        }

        private bool GenerateRoadSideWalkBottom()
        {
            if (_scene_game.Children.OfType<RoadSideWalk>().FirstOrDefault(x => x.IsAnimating == false) is RoadSideWalk roadSidePatch)
            {
                roadSidePatch.IsAnimating = true;

                roadSidePatch.SetPosition(
                    left: (roadSidePatch.Height * -1.5) - 30,
                    top: (Constants.DEFAULT_SCENE_HEIGHT / 5 + roadSidePatch.Height / 2) - 50,
                    z: 0);

                return true;
            }

            return false;
        }

        private bool AnimateRoadSideWalk(Construct roadSidePatch)
        {
            var speed = (_scene_game.Speed + roadSidePatch.SpeedOffset);
            MoveConstructBottomRight(construct: roadSidePatch, speed: speed);
            return true;
        }

        private bool RecycleRoadSideWalk(Construct roadSidePatch)
        {
            var hitBox = roadSidePatch.GetHitBox();

            if (hitBox.Top > Constants.DEFAULT_SCENE_HEIGHT || hitBox.Left - roadSidePatch.Width > Constants.DEFAULT_SCENE_WIDTH)
            {
                roadSidePatch.IsAnimating = false;

                roadSidePatch.SetPosition(
                    left: -3000,
                    top: -3000);
            }

            return true;
        }

        #endregion

        #region RoadSideTree

        private bool SpawnRoadSideTrees()
        {
            for (int i = 0; i < 6; i++)
            {
                RoadSideTree roadSideTree = new(
                    animateAction: AnimateRoadSideTree,
                    recycleAction: RecycleRoadSideTree);

                roadSideTree.SetPosition(
                    left: -3000,
                    top: -3000);

                _scene_game.AddToScene(roadSideTree);

                SpawnDropShadow(source: roadSideTree);
            }

            return true;
        }

        private bool GenerateRoadSideTreeTop()
        {
            //if (_scene_game.Children.OfType<RoadSideTree>().FirstOrDefault(x => x.IsAnimating == false) is RoadSideTree tree2)
            //{
            //    tree2.IsAnimating = true;

            //    tree2.SetPosition(
            //      left: (Constants.DEFAULT_SCENE_WIDTH / 2 - tree2.Width) + 160,
            //      top: (tree2.Height * -1.1) - 55,
            //      z: 2);

            //    SyncDropShadow(tree2);
            //}

            if (_scene_game.Children.OfType<RoadSideTree>().FirstOrDefault(x => x.IsAnimating == false) is RoadSideTree tree)
            {
                tree.IsAnimating = true;

                tree.SetPosition(
                  left: (Constants.DEFAULT_SCENE_WIDTH / 2 - tree.Width),
                  top: (tree.Height * -1.1),
                  z: 3);

                SyncDropShadow(tree);
            }

            return true;
        }

        private bool GenerateRoadSideTreeBottom()
        {
            if (_scene_game.Children.OfType<RoadSideTree>().FirstOrDefault(x => x.IsAnimating == false) is RoadSideTree tree)
            {
                tree.IsAnimating = true;

                tree.SetPosition(
                  left: (-1 * tree.Width),
                  top: (Constants.DEFAULT_SCENE_HEIGHT / 3),
                  z: 4);

                SyncDropShadow(tree);
            }

            //if (_scene_game.Children.OfType<RoadSideTree>().FirstOrDefault(x => x.IsAnimating == false) is RoadSideTree tree2)
            //{
            //    tree2.IsAnimating = true;

            //    tree2.SetPosition(
            //      left: (-1.73 * tree2.Width),
            //      top: (Constants.DEFAULT_SCENE_HEIGHT / 2.5),
            //      z: 4);

            //    SyncDropShadow(tree2);
            //}

            return true;
        }

        private bool AnimateRoadSideTree(Construct roadSideTree)
        {
            var speed = (_scene_game.Speed + roadSideTree.SpeedOffset);
            MoveConstructBottomRight(construct: roadSideTree, speed: speed);
            return true;
        }

        private bool RecycleRoadSideTree(Construct roadSideTree)
        {
            var hitBox = roadSideTree.GetHitBox();

            if (hitBox.Top > Constants.DEFAULT_SCENE_HEIGHT || hitBox.Left - roadSideTree.Width > Constants.DEFAULT_SCENE_WIDTH)
            {
                roadSideTree.IsAnimating = false;

                roadSideTree.SetPosition(
                    left: -3000,
                    top: -3000);
            }

            return true;
        }

        #endregion

        #region RoadSideHedge

        private bool SpawnRoadSideHedges()
        {
            for (int i = 0; i < 11; i++)
            {
                RoadSideHedge hedge = new(
                    animateAction: AnimateRoadSideHedge,
                    recycleAction: RecycleRoadSideHedge);

                hedge.SetPosition(
                    left: -3000,
                    top: -3000);

                _scene_game.AddToScene(hedge);
            }

            return true;
        }

        private bool GenerateRoadSideHedgeTop()
        {
            if (_scene_game.Children.OfType<RoadSideHedge>().FirstOrDefault(x => x.IsAnimating == false) is RoadSideHedge hedge)
            {
                hedge.IsAnimating = true;

                hedge.SetPosition(
                  left: (Constants.DEFAULT_SCENE_WIDTH / 3.8),
                  top: hedge.Height * -1,
                  z: 2);

                return true;
            }

            return false;
        }

        private bool GenerateRoadSideHedgeBottom()
        {
            if (_scene_game.Children.OfType<RoadSideHedge>().FirstOrDefault(x => x.IsAnimating == false) is RoadSideHedge hedge)
            {
                hedge.IsAnimating = true;

                hedge.SetPosition(
                  left: -1 * hedge.Width,
                  top: (Constants.DEFAULT_SCENE_HEIGHT / 3.1),
                  z: 3);

                return true;
            }

            return false;
        }

        private bool AnimateRoadSideHedge(Construct hedge)
        {
            var speed = (_scene_game.Speed + hedge.SpeedOffset);
            MoveConstructBottomRight(construct: hedge, speed: speed);
            return true;
        }

        private bool RecycleRoadSideHedge(Construct hedge)
        {
            var hitBox = hedge.GetHitBox();

            if (hitBox.Top > Constants.DEFAULT_SCENE_HEIGHT || hitBox.Left - hedge.Width > Constants.DEFAULT_SCENE_WIDTH)
            {
                hedge.IsAnimating = false;

                hedge.SetPosition(
                    left: -3000,
                    top: -3000);
            }

            return true;
        }

        #endregion

        #region RoadSideLamp

        private bool SpawnRoadSideLamps()
        {
            for (int i = 0; i < 3; i++)
            {
                RoadSideLamp roadSideLamp = new(
                    animateAction: AnimateRoadSideLamp,
                    recycleAction: RecycleRoadSideLamp);

                roadSideLamp.SetPosition(
                    left: -3000,
                    top: -3000);

                _scene_game.AddToScene(roadSideLamp);

                SpawnDropShadow(source: roadSideLamp);
            }

            return true;
        }

        private bool GenerateRoadSideLampTop()
        {
            if (_scene_game.Children.OfType<RoadSideLamp>().FirstOrDefault(x => x.IsAnimating == false) is RoadSideLamp roadSideLamp)
            {
                roadSideLamp.IsAnimating = true;

                roadSideLamp.SetPosition(
                  left: (Constants.DEFAULT_SCENE_WIDTH / 2.40 - roadSideLamp.Width) + 10,
                  top: ((roadSideLamp.Height * 1.5) * -1) + 5,
                  z: 3);

                SyncDropShadow(roadSideLamp);

                LoggerExtensions.Log("RoadSideLamp generated.");

                return true;
            }

            return false;
        }

        private bool GenerateRoadSideLampBottom()
        {
            if (_scene_game.Children.OfType<RoadSideLamp>().FirstOrDefault(x => x.IsAnimating == false) is RoadSideLamp tree)
            {
                tree.IsAnimating = true;

                tree.SetPosition(
                  left: (-1.9 * tree.Width),
                  top: (Constants.DEFAULT_SCENE_HEIGHT / 3),
                  z: 4);

                SyncDropShadow(tree);

                return true;
            }

            return false;
        }

        private bool AnimateRoadSideLamp(Construct roadSideLamp)
        {
            var speed = (_scene_game.Speed + roadSideLamp.SpeedOffset);
            MoveConstructBottomRight(construct: roadSideLamp, speed: speed);
            return true;
        }

        private bool RecycleRoadSideLamp(Construct roadSideLamp)
        {
            var hitBox = roadSideLamp.GetHitBox();

            if (hitBox.Top > Constants.DEFAULT_SCENE_HEIGHT || hitBox.Left - roadSideLamp.Width > Constants.DEFAULT_SCENE_WIDTH)
            {
                roadSideLamp.IsAnimating = false;

                roadSideLamp.SetPosition(
                    left: -3000,
                    top: -3000);
            }

            return true;
        }

        #endregion

        #region RoadSideBillboard

        private bool SpawnRoadSideBillboards()
        {
            for (int i = 0; i < 3; i++)
            {
                RoadSideBillboard roadSideBillboard = new(
                    animateAction: AnimateRoadSideBillboard,
                    recycleAction: RecycleRoadSideBillboard);

                roadSideBillboard.SetPosition(
                    left: -3000,
                    top: -3000);

                _scene_game.AddToScene(roadSideBillboard);

                SpawnDropShadow(source: roadSideBillboard);
            }

            return true;
        }

        private bool GenerateRoadSideBillboardTop()
        {
            if (_scene_game.Children.OfType<RoadSideBillboard>().FirstOrDefault(x => x.IsAnimating == false) is RoadSideBillboard roadSideBillboard)
            {
                roadSideBillboard.IsAnimating = true;

                roadSideBillboard.SetPosition(
                  left: (Constants.DEFAULT_SCENE_WIDTH / 2.5 - roadSideBillboard.Width) + 48,
                  top: ((roadSideBillboard.Height * 1.5) * -1) - 10,
                  z: 4);

                SyncDropShadow(roadSideBillboard);

                LoggerExtensions.Log("RoadSideBillboard generated.");

                return true;
            }

            return false;
        }

        private bool GenerateRoadSideBillboardBottom()
        {
            if (_scene_game.Children.OfType<RoadSideBillboard>().FirstOrDefault(x => x.IsAnimating == false) is RoadSideBillboard tree)
            {
                tree.IsAnimating = true;

                tree.SetPosition(
                  left: (-1.9 * tree.Width),
                  top: (Constants.DEFAULT_SCENE_HEIGHT / 3),
                  z: 4);

                SyncDropShadow(tree);

                return true;
            }

            return false;
        }

        private bool AnimateRoadSideBillboard(Construct roadSideBillboard)
        {
            var speed = (_scene_game.Speed + roadSideBillboard.SpeedOffset);
            MoveConstructBottomRight(construct: roadSideBillboard, speed: speed);
            return true;
        }

        private bool RecycleRoadSideBillboard(Construct roadSideBillboard)
        {
            var hitBox = roadSideBillboard.GetHitBox();

            if (hitBox.Top > Constants.DEFAULT_SCENE_HEIGHT || hitBox.Left - roadSideBillboard.Width > Constants.DEFAULT_SCENE_WIDTH)
            {
                roadSideBillboard.IsAnimating = false;

                roadSideBillboard.SetPosition(
                    left: -3000,
                    top: -3000);
            }

            return true;
        }

        #endregion

        #region Honk

        private bool SpawnHonks()
        {
            for (int i = 0; i < 10; i++)
            {
                Honk honk = new(
                    animateAction: AnimateHonk,
                    recycleAction: RecycleHonk);

                honk.SetPosition(
                    left: -3000,
                    top: -3000);

                _scene_game.AddToScene(honk);
            }

            return true;
        }

        private bool GenerateHonk(Construct source)
        {
            if (_scene_game.Children.OfType<Honk>().FirstOrDefault(x => x.IsAnimating == false) is Honk honk)
            {
                honk.IsAnimating = true;
                honk.SetPopping();

                honk.Reset();

                var hitBox = source.GetCloseHitBox();

                honk.Reposition(source: source);
                honk.SetRotation(_random.Next(-30, 30));
                honk.SetZ(source.GetZ() + 1);

                source.SetPopping();

                return true;
            }

            return false;
        }

        private bool AnimateHonk(Construct honk)
        {
            honk.Pop();
            honk.Fade(0.06);
            return true;
        }

        private bool RecycleHonk(Construct honk)
        {
            if (honk.IsFadingComplete)
            {
                honk.IsAnimating = false;

                honk.SetPosition(
                    left: -3000,
                    top: -3000);
            }

            return true;
        }

        private bool GenerateVehicleBossHonk(VehicleBoss source)
        {
            // if there are no UfoBosses or enemies in the scene the vehicles will honk

            if (_scene_game.SceneState == SceneState.GAME_RUNNING && !UfoBossExists())
            {
                return GenerateHonk(source);
            }

            return true;
        }

        private bool GenerateVehicleHonk(Vehicle source)
        {
            // if there are no UfoBosses or enemies in the scene the vehicles will honk

            if (_scene_game.SceneState == SceneState.GAME_RUNNING && !UfoBossExists() && !UfoEnemyExists() && !VehicleBossExists())
            {
                return GenerateHonk(source);
            }

            return true;
        }

        private bool GenerateUfoEnemyHonk(UfoEnemy source)
        {
            // if there are no UfoBosses in the scene the vehicles will honk

            if (_scene_game.SceneState == SceneState.GAME_RUNNING && !UfoBossExists())
            {
                return GenerateHonk(source);
            }

            return true;
        }

        #endregion

        #region Cloud

        private bool SpawnClouds()
        {
            for (int i = 0; i < 5; i++)
            {
                Cloud cloud = new(
                    animateAction: AnimateCloud,
                    recycleAction: RecycleCloud);

                cloud.SetPosition(
                    left: -3000,
                    top: -3000,
                    z: 9);

                _scene_game.AddToScene(cloud);
            }

            return true;
        }

        private bool GenerateCloud()
        {
            if (_scene_game.Children.OfType<Cloud>().FirstOrDefault(x => x.IsAnimating == false) is Cloud cloud)
            {
                cloud.IsAnimating = true;
                cloud.Reset();

                var topOrLeft = _random.Next(2);

                var lane = _random.Next(2);

                switch (topOrLeft)
                {
                    case 0:
                        {
                            var xLaneWidth = Constants.DEFAULT_SCENE_WIDTH / 4;
                            cloud.SetPosition(
                                left: _random.Next(Convert.ToInt32(xLaneWidth - cloud.Width)),
                                top: cloud.Height * -1);
                        }
                        break;
                    case 1:
                        {
                            var yLaneWidth = (Constants.DEFAULT_SCENE_HEIGHT / 2) / 2;
                            cloud.SetPosition(
                                left: cloud.Width * -1,
                                top: _random.Next(Convert.ToInt32(yLaneWidth)));
                        }
                        break;
                    default:
                        break;
                }

                return true;
            }

            return false;
        }

        private bool AnimateCloud(Construct cloud)
        {
            Cloud cloud1 = cloud as Cloud;
            cloud1.Hover();

            var speed = (_scene_game.Speed + cloud.SpeedOffset);
            MoveConstructBottomRight(construct: cloud, speed: speed);
            return true;
        }

        private bool RecycleCloud(Construct cloud)
        {
            var hitBox = cloud.GetHitBox();

            if (hitBox.Top > Constants.DEFAULT_SCENE_HEIGHT || hitBox.Left > Constants.DEFAULT_SCENE_WIDTH)
            {
                cloud.IsAnimating = false;

                cloud.SetPosition(
                    left: -3000,
                    top: -3000);

            }

            return true;
        }

        #endregion

        #region UfoBoss

        private bool SpawnUfoBosses()
        {
            UfoBoss ufoBoss = new(
                animateAction: AnimateUfoBoss,
                recycleAction: RecycleUfoBoss);

            ufoBoss.SetPosition(
                left: -3000,
                top: -3000,
                z: 8);

            _scene_game.AddToScene(ufoBoss);

            SpawnDropShadow(source: ufoBoss);

            return true;
        }

        private bool GenerateUfoBoss()
        {
            // if scene doesn't contain a UfoBoss then pick a UfoBoss and add to scene

            if (_scene_game.SceneState == SceneState.GAME_RUNNING &&
                _ufo_boss_threashold.ShouldRelease(_game_score_bar.GetScore()) && !UfoBossExists() &&
                _scene_game.Children.OfType<UfoBoss>().FirstOrDefault(x => x.IsAnimating == false) is UfoBoss ufoBoss)
            {
                _audio_stub.Stop(SoundType.GAME_BACKGROUND_MUSIC);
                //_audio_stub.Play(SoundType.UFO_BOSS_BACKGROUND_MUSIC);
                _audio_stub.SetVolume(SoundType.AMBIENCE, 0.2);

                ufoBoss.IsAnimating = true;
                ufoBoss.Reset();
                ufoBoss.SetPosition(
                    left: 0,
                    top: ufoBoss.Height * -1);

                SyncDropShadow(ufoBoss);

                // set UfoBoss health
                ufoBoss.Health = _ufo_boss_threashold.GetReleasePointDifference() * 1.5;

                _ufo_boss_threashold.IncreaseThreasholdLimit(increment: _ufo_boss_threashold_limit_increase, currentPoint: _game_score_bar.GetScore());

                _ufo_boss_health_bar.SetMaxiumHealth(ufoBoss.Health);
                _ufo_boss_health_bar.SetValue(ufoBoss.Health);
                _ufo_boss_health_bar.SetIcon(ufoBoss.GetContentUri());
                _ufo_boss_health_bar.SetBarForegroundColor(color: Colors.Crimson);

                _scene_game.ActivateSlowMotion();

                GenerateInterimScreen("Beware of Psycho Rocket");

                return true;
            }

            return false;
        }

        private bool AnimateUfoBoss(Construct ufoBoss)
        {
            UfoBoss ufoBoss1 = ufoBoss as UfoBoss;

            if (ufoBoss1.IsDead)
            {
                ufoBoss.Shrink();
            }
            else
            {
                ufoBoss.Pop();

                ufoBoss1.Hover();
                ufoBoss1.DepleteHitStance();
                ufoBoss1.DepleteWinStance();

                if (_scene_game.SceneState == SceneState.GAME_RUNNING)
                {
                    var speed = (_scene_game.Speed + ufoBoss.SpeedOffset);
                    var scaling = ScreenExtensions.GetScreenSpaceScaling();

                    if (ufoBoss1.IsAttacking)
                    {
                        ufoBoss1.Move(
                            speed: speed,
                            sceneWidth: Constants.DEFAULT_SCENE_WIDTH * scaling,
                            sceneHeight: Constants.DEFAULT_SCENE_HEIGHT * scaling,
                            playerPoint: _player.GetCloseHitBox());
                    }
                    else
                    {
                        MoveConstructBottomRight(construct: ufoBoss, speed: speed);

                        if (ufoBoss.GetLeft() > (Constants.DEFAULT_SCENE_WIDTH * scaling / 3)) // bring UfoBoss to a suitable distance from player and then start attacking
                        {
                            ufoBoss1.IsAttacking = true;
                        }
                    }
                }
            }

            return true;
        }

        private bool RecycleUfoBoss(Construct ufoBoss)
        {
            if (ufoBoss.IsShrinkingComplete)
            {
                ufoBoss.IsAnimating = false;

                ufoBoss.SetPosition(
                    left: -3000,
                    top: -3000);
            }

            return true;
        }

        private void LooseUfoBossHealth(UfoBoss ufoBoss)
        {
            ufoBoss.SetPopping();
            ufoBoss.LooseHealth();
            ufoBoss.SetHitStance();

            _ufo_boss_health_bar.SetValue(ufoBoss.Health);

            if (ufoBoss.IsDead && ufoBoss.IsAttacking)
            {
                //_audio_stub.Stop(SoundType.UFO_BOSS_BACKGROUND_MUSIC);
                _audio_stub.Play(SoundType.GAME_BACKGROUND_MUSIC);
                _audio_stub.SetVolume(SoundType.AMBIENCE, 0.6);

                ufoBoss.IsAttacking = false;

                _player.SetWinStance();
                _game_score_bar.GainScore(5);

                GenerateInterimScreen("Psycho Rocket Busted");

                _scene_game.ActivateSlowMotion();
            }
        }

        private bool UfoBossExists()
        {
            return _scene_game.Children.OfType<UfoBoss>().Any(x => x.IsAnimating);
        }

        #endregion

        #region VehicleBoss

        private bool SpawnVehicleBosses()
        {
            VehicleBoss vehicleBoss = new(
                animateAction: AnimateVehicleBoss,
                recycleAction: RecycleVehicleBoss);

            vehicleBoss.SetPosition(
                left: -3000,
                top: -3000,
                z: 3);

            _scene_game.AddToScene(vehicleBoss);

            return true;
        }

        private bool GenerateVehicleBoss()
        {
            // if scene doesn't contain a VehicleBoss then pick a random VehicleBoss and add to scene

            if (_scene_game.SceneState == SceneState.GAME_RUNNING &&
                _vehicle_boss_threashold.ShouldRelease(_game_score_bar.GetScore()) && !VehicleBossExists() &&
                _scene_game.Children.OfType<VehicleBoss>().FirstOrDefault(x => x.IsAnimating == false) is VehicleBoss vehicleBoss)
            {
                _audio_stub.Stop(SoundType.GAME_BACKGROUND_MUSIC);
                _audio_stub.Play(SoundType.UFO_BOSS_BACKGROUND_MUSIC);
                _audio_stub.SetVolume(SoundType.AMBIENCE, 0.4);

                vehicleBoss.IsAnimating = true;
                vehicleBoss.Reset();
                vehicleBoss.Reposition();
                vehicleBoss.SetZ(3);

                // set VehicleBoss health
                vehicleBoss.Health = _vehicle_boss_threashold.GetReleasePointDifference() * 1.5;

                _vehicle_boss_threashold.IncreaseThreasholdLimit(increment: _vehicle_boss_threashold_limit_increase, currentPoint: _game_score_bar.GetScore());

                _vehicle_boss_health_bar.SetMaxiumHealth(vehicleBoss.Health);
                _vehicle_boss_health_bar.SetValue(vehicleBoss.Health);
                _vehicle_boss_health_bar.SetIcon(vehicleBoss.GetContentUri());
                _vehicle_boss_health_bar.SetBarForegroundColor(color: Colors.Crimson);

                GenerateInterimScreen("Crazy Honker Arrived");
                _scene_game.ActivateSlowMotion();

                return true;
            }

            return false;
        }

        private bool AnimateVehicleBoss(Construct vehicleBoss)
        {
            VehicleBoss vehicleBoss1 = vehicleBoss as VehicleBoss;

            var speed = (_scene_game.Speed + vehicleBoss.SpeedOffset);

            if (vehicleBoss1.IsDead)
            {
                MoveConstructBottomRight(vehicleBoss, speed);
            }
            else
            {
                vehicleBoss.Pop();

                if (_scene_game.SceneState == SceneState.GAME_RUNNING)
                {
                    var scaling = ScreenExtensions.GetScreenSpaceScaling();

                    if (vehicleBoss1.IsAttacking)
                    {
                        vehicleBoss1.Move(
                            speed: speed,
                            sceneWidth: Constants.DEFAULT_SCENE_WIDTH * scaling,
                            sceneHeight: Constants.DEFAULT_SCENE_HEIGHT * scaling);

                        if (vehicleBoss1.Honk())
                            GenerateVehicleBossHonk(vehicleBoss1);
                    }
                    else
                    {
                        if (_scene_game.Children.OfType<Vehicle>().All(x => !x.IsAnimating) || _scene_game.Children.OfType<Vehicle>().Where(x => x.IsAnimating).All(x => x.GetLeft() > Constants.DEFAULT_SCENE_WIDTH * scaling / 2)) // only bring the boss in view when all other vechiles are gone
                        {
                            MoveConstructBottomRight(construct: vehicleBoss, speed: speed);

                            if (vehicleBoss.GetLeft() > ((Constants.DEFAULT_SCENE_WIDTH * scaling) / 4 * 3)) // bring VehicleBoss to a suitable distance from player and then start attacking
                            {
                                vehicleBoss1.IsAttacking = true;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private bool RecycleVehicleBoss(Construct vehicleBoss)
        {
            var hitBox = vehicleBoss.GetHitBox();

            VehicleBoss vehicleBoss1 = vehicleBoss as VehicleBoss;

            if (vehicleBoss1.IsDead && hitBox.Top > Constants.DEFAULT_SCENE_HEIGHT || hitBox.Left > Constants.DEFAULT_SCENE_WIDTH)
            {
                vehicleBoss.IsAnimating = false;

                vehicleBoss.SetPosition(
                    left: -3000,
                    top: -3000);
            }

            return true;
        }

        private void LooseVehicleBossHealth(VehicleBoss vehicleBoss)
        {
            vehicleBoss.SetPopping();
            vehicleBoss.LooseHealth();

            _vehicle_boss_health_bar.SetValue(vehicleBoss.Health);

            if (vehicleBoss.IsDead && vehicleBoss.IsAttacking)
            {
                _audio_stub.Stop(SoundType.UFO_BOSS_BACKGROUND_MUSIC);

                _audio_stub.Play(SoundType.GAME_BACKGROUND_MUSIC);

                _audio_stub.SetVolume(SoundType.AMBIENCE, 0.6);

                vehicleBoss.IsAttacking = false;

                _player.SetWinStance();
                _game_score_bar.GainScore(5);

                GenerateInterimScreen("Crazy Honker Busted");

                _scene_game.ActivateSlowMotion();
            }
        }

        private bool VehicleBossExists()
        {
            return _scene_game.Children.OfType<VehicleBoss>().Any(x => x.IsAnimating);
        }

        #endregion

        #region UfoEnemy

        private bool SpawnUfoEnemys()
        {
            for (int i = 0; i < 7; i++)
            {
                UfoEnemy ufoEnemy = new(
                    animateAction: AnimateUfoEnemy,
                    recycleAction: RecycleUfoEnemy);

                _scene_game.AddToScene(ufoEnemy);

                ufoEnemy.SetPosition(
                    left: -3000,
                    top: -3000,
                    z: 8);

                SpawnDropShadow(source: ufoEnemy);
            }

            return true;
        }

        private bool GenerateUfoEnemy()
        {
            if (!UfoBossExists() &&
                _enemy_threashold.ShouldRelease(_game_score_bar.GetScore()) &&
                _scene_game.Children.OfType<UfoEnemy>().FirstOrDefault(x => x.IsAnimating == false) is UfoEnemy enemy)
            {
                enemy.IsAnimating = true;
                enemy.Reset();

                var topOrLeft = _random.Next(2);

                switch (topOrLeft)
                {
                    case 0:
                        {
                            var xLaneWidth = Constants.DEFAULT_SCENE_WIDTH / 2;

                            enemy.SetPosition(
                                left: _random.Next((int)(xLaneWidth - enemy.Width)),
                                top: enemy.Height * -1);
                        }
                        break;
                    case 1:
                        {
                            var yLaneWidth = Constants.DEFAULT_SCENE_HEIGHT / 2;

                            enemy.SetPosition(
                                left: enemy.Width * -1,
                                top: _random.Next((int)(yLaneWidth - enemy.Height)));
                        }
                        break;
                    default:
                        break;
                }

                SyncDropShadow(enemy);

                if (!_enemy_fleet_appeared)
                {
                    _audio_stub.Play(SoundType.UFO_ENEMY_ENTRY);

                    GenerateInterimScreen("Beware of Aliens");
                    _scene_game.ActivateSlowMotion();
                    _enemy_fleet_appeared = true;
                }

                return true;
            }

            return false;
        }

        private bool AnimateUfoEnemy(Construct ufoEnemy)
        {
            UfoEnemy enemy1 = ufoEnemy as UfoEnemy;

            if (enemy1.IsDead)
            {
                enemy1.Shrink();
            }
            else
            {
                enemy1.Hover();
                enemy1.Pop();

                var speed = _scene_game.Speed + ufoEnemy.SpeedOffset;

                MoveConstructBottomRight(construct: ufoEnemy, speed: speed);

                if (_scene_game.SceneState == SceneState.GAME_RUNNING)
                {
                    if (enemy1.Honk())
                        GenerateUfoEnemyHonk(enemy1);

                    if (enemy1.Attack())
                        GenerateUfoEnemyRocket(enemy1);
                }
            }

            return true;
        }

        private bool RecycleUfoEnemy(Construct ufoEnemy)
        {
            var hitbox = ufoEnemy.GetHitBox();

            // enemy is dead or goes out of bounds
            if (ufoEnemy.IsShrinkingComplete ||
                hitbox.Left > Constants.DEFAULT_SCENE_WIDTH || hitbox.Top > Constants.DEFAULT_SCENE_HEIGHT ||
                hitbox.Right < 0 || hitbox.Bottom < 0)
            {
                ufoEnemy.IsAnimating = false;

                ufoEnemy.SetPosition(
                    left: -3000,
                    top: -3000);

                LoggerExtensions.Log("UfoEnemy Recycled");
            }

            return true;
        }

        private void LooseUfoEnemyHealth(UfoEnemy ufoEnemy)
        {
            ufoEnemy.SetPopping();
            ufoEnemy.LooseHealth();

            if (ufoEnemy.IsDead)
            {
                _game_score_bar.GainScore(3);

                _enemy_kill_count++;

                // after killing 15 enemies increase the threadhold limit
                if (_enemy_kill_count > _enemy_kill_count_limit)
                {
                    _enemy_threashold.IncreaseThreasholdLimit(increment: _enemy_threashold_limit_increase, currentPoint: _game_score_bar.GetScore());
                    _enemy_kill_count = 0;
                    _enemy_fleet_appeared = false;

                    GenerateInterimScreen("Alien Fleet Vanquished");
                    _scene_game.ActivateSlowMotion();
                }

                LoggerExtensions.Log("UfoEnemy dead");
            }
        }

        private bool UfoEnemyExists()
        {
            return _scene_game.Children.OfType<UfoEnemy>().Any(x => x.IsAnimating);
        }

        #endregion

        #region PlayerFireCracker

        private bool SpawnPlayerFireCrackers()
        {
            for (int i = 0; i < 3; i++)
            {
                PlayerFireCracker playerFireCracker = new(
                    animateAction: AnimatePlayerFireCracker,
                    recycleAction: RecyclePlayerFireCracker);

                playerFireCracker.SetPosition(
                    left: -3000,
                    top: -3000,
                    z: 7);

                _scene_game.AddToScene(playerFireCracker);

                SpawnDropShadow(source: playerFireCracker);
            }

            return true;
        }

        private bool GeneratePlayerFireCracker()
        {
            if (_scene_game.SceneState == SceneState.GAME_RUNNING && !_scene_game.IsSlowMotionActivated)
            {
                if ((VehicleBossExists() || _scene_game.Children.OfType<Vehicle>().Any(x => x.IsAnimating)) &&
                    _scene_game.Children.OfType<PlayerFireCracker>().FirstOrDefault(x => x.IsAnimating == false) is PlayerFireCracker playerFireCracker)
                {
                    _player.SetAttackStance();

                    playerFireCracker.Reset();
                    playerFireCracker.IsAnimating = true;
                    playerFireCracker.IsGravitating = true;
                    playerFireCracker.SetPopping();

                    playerFireCracker.SetRotation(_random.Next(-30, 30));

                    playerFireCracker.Reposition(
                        player: _player);

                    SyncDropShadow(playerFireCracker);

                    LoggerExtensions.Log("Player Ground Bomb dropped.");

                    return true;
                }
                else
                {
                    _player.SetWinStance();
                }
            }

            return false;
        }

        private bool AnimatePlayerFireCracker(Construct playerFireCracker)
        {
            PlayerFireCracker playerFireCracker1 = playerFireCracker as PlayerFireCracker;

            var speed = (_scene_game.Speed + playerFireCracker.SpeedOffset);

            if (playerFireCracker1.IsBlasting)
            {
                playerFireCracker.Expand();
                playerFireCracker.Fade(0.02);

                MoveConstructBottomRight(construct: playerFireCracker, speed: speed);
            }
            else
            {
                playerFireCracker.Pop();

                playerFireCracker.SetLeft(playerFireCracker.GetLeft() + speed);
                playerFireCracker.SetTop(playerFireCracker.GetTop() + speed * 1.2);

                if (_scene_game.SceneState == SceneState.GAME_RUNNING)
                {
                    DropShadow dropShadow = _scene_game.Children.OfType<DropShadow>().First(x => x.Id == playerFireCracker.Id);

                    var drpShdwHitBox = dropShadow.GetCloseHitBox();
                    var fireCrackerHitBox = playerFireCracker.GetCloseHitBox();

                    // start blast animation when the bomb touches it's shadow
                    if (drpShdwHitBox.IntersectsWith(fireCrackerHitBox) && playerFireCracker.GetBottom() > dropShadow.GetBottom())
                    {
                        // while in blast check if it intersects with any vehicle, if it does then the vehicle stops honking and slows down
                        if (_scene_game.Children.OfType<Vehicle>()
                            .Where(x => x.IsAnimating && x.WillHonk)
                            .FirstOrDefault(x => x.GetCloseHitBox().IntersectsWith(fireCrackerHitBox)) is Vehicle vehicle)
                        {
                            vehicle.SetBlast();
                            _game_score_bar.GainScore(3);
                        }

                        // if a vechile boss is in place then boss looses health
                        if (_scene_game.Children.OfType<VehicleBoss>()
                            .FirstOrDefault(x => x.IsAnimating && x.IsAttacking) is VehicleBoss vehicleBoss && vehicleBoss.GetCloseHitBox().IntersectsWith(fireCrackerHitBox))
                        {
                            LooseVehicleBossHealth(vehicleBoss);
                        }

                        playerFireCracker1.SetBlast();
                    }
                }
            }

            return true;
        }

        private bool RecyclePlayerFireCracker(Construct playerFireCracker)
        {
            if (playerFireCracker.IsFadingComplete)
            {
                playerFireCracker.IsAnimating = false;
                playerFireCracker.IsGravitating = false;

                playerFireCracker.SetPosition(
                    left: -3000,
                    top: -3000);

                return true;
            }

            return false;
        }

        #endregion

        #region PlayerRocket

        private bool SpawnPlayerRockets()
        {
            for (int i = 0; i < 4; i++)
            {
                PlayerRocket playerRocket = new(
                    animateAction: AnimatePlayerRocket,
                    recycleAction: RecyclePlayerRocket);

                playerRocket.SetPosition(
                    left: -3000,
                    top: -3000,
                    z: 7);

                _scene_game.AddToScene(playerRocket);

                SpawnDropShadow(source: playerRocket);
            }

            return true;
        }

        private bool GeneratePlayerRocket()
        {
            if (_scene_game.SceneState == SceneState.GAME_RUNNING && !_scene_game.IsSlowMotionActivated &&
                _scene_game.Children.OfType<PlayerRocket>().FirstOrDefault(x => x.IsAnimating == false) is PlayerRocket playerRocket)
            {
                _player.SetAttackStance();

                playerRocket.Reset();
                playerRocket.IsAnimating = true;
                playerRocket.SetPopping();

                playerRocket.Reposition(
                    Player: _player);

                SyncDropShadow(playerRocket);

                var playerDistantHitBox = _player.GetDistantHitBox();

                // get closest possible target
                UfoBossRocketSeeking UfoBossRocketSeeking = _scene_game.Children.OfType<UfoBossRocketSeeking>()?.FirstOrDefault(x => x.IsAnimating && !x.IsBlasting && x.GetHitBox().IntersectsWith(playerDistantHitBox));
                UfoBoss UfoBoss = _scene_game.Children.OfType<UfoBoss>()?.FirstOrDefault(x => x.IsAnimating && x.IsAttacking && x.GetHitBox().IntersectsWith(playerDistantHitBox));
                UfoEnemy enemy = _scene_game.Children.OfType<UfoEnemy>()?.FirstOrDefault(x => x.IsAnimating && !x.IsFadingComplete && x.GetHitBox().IntersectsWith(playerDistantHitBox));

                // if not found then find random target
                UfoBossRocketSeeking ??= _scene_game.Children.OfType<UfoBossRocketSeeking>().FirstOrDefault(x => x.IsAnimating && !x.IsBlasting);
                UfoBoss ??= _scene_game.Children.OfType<UfoBoss>().FirstOrDefault(x => x.IsAnimating && x.IsAttacking);
                enemy ??= _scene_game.Children.OfType<UfoEnemy>().FirstOrDefault(x => x.IsAnimating && !x.IsFadingComplete);

                LoggerExtensions.Log("Player Bomb dropped.");

                if (enemy is not null)
                {
                    SetPlayerRocketDirection(source: _player, rocket: playerRocket, rocketTarget: enemy);
                }
                else if (UfoBoss is not null)
                {
                    SetPlayerRocketDirection(source: _player, rocket: playerRocket, rocketTarget: UfoBoss);
                }
                else if (UfoBossRocketSeeking is not null)
                {
                    SetPlayerRocketDirection(source: _player, rocket: playerRocket, rocketTarget: UfoBossRocketSeeking);
                }

                return true;
            }

            return false;
        }

        private bool AnimatePlayerRocket(Construct playerRocket)
        {
            PlayerRocket playerRocket1 = playerRocket as PlayerRocket;

            var hitBox = playerRocket.GetCloseHitBox();

            var speed = (_scene_game.Speed + playerRocket.SpeedOffset);

            if (playerRocket1.AwaitMoveDownLeft)
            {
                playerRocket1.MoveDownLeft(speed);
            }
            else if (playerRocket1.AwaitMoveUpRight)
            {
                playerRocket1.MoveUpRight(speed);
            }
            else if (playerRocket1.AwaitMoveUpLeft)
            {
                playerRocket1.MoveUpLeft(speed);
            }
            else if (playerRocket1.AwaitMoveDownRight)
            {
                playerRocket1.MoveDownRight(speed);
            }

            if (playerRocket1.IsBlasting)
            {
                playerRocket.Expand();
                playerRocket.Fade(0.02);
            }
            else
            {
                playerRocket.Pop();
                playerRocket1.Hover();

                if (_scene_game.SceneState == SceneState.GAME_RUNNING)
                {
                    // if player bomb touches UfoBoss, it blasts, UfoBoss looses health
                    if (_scene_game.Children.OfType<UfoBoss>().FirstOrDefault(x => x.IsAnimating && x.IsAttacking && x.GetCloseHitBox().IntersectsWith(hitBox)) is UfoBoss ufoBoss)
                    {
                        playerRocket1.SetBlast();
                        LooseUfoBossHealth(ufoBoss);
                    }

                    // if player bomb touches UfoBoss's seeking bomb, it blasts
                    if (_scene_game.Children.OfType<UfoBossRocketSeeking>().FirstOrDefault(x => x.IsAnimating && !x.IsBlasting && x.GetCloseHitBox().IntersectsWith(hitBox)) is UfoBossRocketSeeking ufoBossRocketSeeking)
                    {
                        playerRocket1.SetBlast();
                        ufoBossRocketSeeking.SetBlast();
                    }

                    // if player bomb touches enemy, it blasts, enemy looses health
                    if (_scene_game.Children.OfType<UfoEnemy>().FirstOrDefault(x => x.IsAnimating && !x.IsDead && x.GetCloseHitBox().IntersectsWith(hitBox)) is UfoEnemy ufoEnemy)
                    {
                        playerRocket1.SetBlast();
                        LooseUfoEnemyHealth(ufoEnemy);
                    }

                    if (playerRocket1.AutoBlast())
                        playerRocket1.SetBlast();
                }
            }

            return true;
        }

        private bool RecyclePlayerRocket(Construct playerRocket)
        {
            var hitbox = playerRocket.GetHitBox();

            // if bomb is blasted and faed or goes out of scene bounds
            if (playerRocket.IsFadingComplete || hitbox.Left > Constants.DEFAULT_SCENE_WIDTH || hitbox.Right < 0 /*|| hitbox.Top < 0 || hitbox.Top > Constants.DEFAULT_SCENE_HEIGHT*/)
            {
                playerRocket.IsAnimating = false;

                playerRocket.SetPosition(
                    left: -3000,
                    top: -3000);

                return true;
            }

            return false;
        }

        #endregion

        #region UfoEnemyRocket

        private bool SpawnUfoEnemyRockets()
        {
            for (int i = 0; i < 8; i++)
            {
                UfoEnemyRocket ufoEnemyRocket = new(
                    animateAction: AnimateUfoEnemyRocket,
                    recycleAction: RecycleUfoEnemyRocket);

                ufoEnemyRocket.SetPosition(
                    left: -3000,
                    top: -3000,
                    z: 8);

                _scene_game.AddToScene(ufoEnemyRocket);

                SpawnDropShadow(source: ufoEnemyRocket);
            }

            return true;
        }

        private bool GenerateUfoEnemyRocket(UfoEnemy ufoEnemy)
        {
            if (_scene_game.SceneState == SceneState.GAME_RUNNING &&
                _scene_game.Children.OfType<UfoEnemyRocket>().FirstOrDefault(x => x.IsAnimating == false) is UfoEnemyRocket enemyRocket)
            {
                enemyRocket.Reset();
                enemyRocket.IsAnimating = true;
                enemyRocket.SetPopping();

                enemyRocket.Reposition(ufoEnemy: ufoEnemy);

                SyncDropShadow(enemyRocket);

                LoggerExtensions.Log("UfoEnemy Bomb dropped.");

                return true;
            }

            return false;
        }

        private bool AnimateUfoEnemyRocket(Construct ufoEnemyRocket)
        {
            UfoEnemyRocket enemyRocket = ufoEnemyRocket as UfoEnemyRocket;

            var speed = _scene_game.Speed + ufoEnemyRocket.SpeedOffset;

            MoveConstructBottomRight(construct: enemyRocket, speed: speed);

            if (enemyRocket.IsBlasting)
            {
                ufoEnemyRocket.Expand();
                ufoEnemyRocket.Fade(0.02);
            }
            else
            {
                ufoEnemyRocket.Pop();
                enemyRocket.Hover();

                if (_scene_game.SceneState == SceneState.GAME_RUNNING)
                {
                    if (enemyRocket.GetCloseHitBox().IntersectsWith(_player.GetCloseHitBox()))
                    {
                        enemyRocket.SetBlast();
                        LoosePlayerHealth();
                    }

                    if (enemyRocket.AutoBlast())
                        enemyRocket.SetBlast();
                }
            }

            return true;
        }

        private bool RecycleUfoEnemyRocket(Construct ufoEnemyRocket)
        {
            var hitbox = ufoEnemyRocket.GetHitBox();

            // if bomb is blasted and faed or goes out of scene bounds
            if (ufoEnemyRocket.IsFadingComplete || hitbox.Left > Constants.DEFAULT_SCENE_WIDTH || hitbox.Right < 0 || hitbox.Top < 0 || hitbox.Bottom > Constants.DEFAULT_SCENE_HEIGHT)
            {
                ufoEnemyRocket.IsAnimating = false;

                ufoEnemyRocket.SetPosition(
                    left: -3000,
                    top: -3000);

                return true;
            }

            return false;
        }

        #endregion

        #region UfoBossRocket

        private bool SpawnUfoBossRockets()
        {
            for (int i = 0; i < 4; i++)
            {
                UfoBossRocket ufoBossRocket = new(
                    animateAction: AnimateUfoBossRocket,
                    recycleAction: RecycleUfoBossRocket);

                ufoBossRocket.SetPosition(
                    left: -3000,
                    top: -3000,
                    z: 7);

                _scene_game.AddToScene(ufoBossRocket);

                SpawnDropShadow(source: ufoBossRocket);
            }

            return true;
        }

        private bool GenerateUfoBossRocket()
        {
            if (_scene_game.SceneState == SceneState.GAME_RUNNING &&
                _scene_game.Children.OfType<UfoBoss>().FirstOrDefault(x => x.IsAnimating && x.IsAttacking) is UfoBoss ufoBoss &&
                _scene_game.Children.OfType<UfoBossRocket>().FirstOrDefault(x => x.IsAnimating == false) is UfoBossRocket ufoBossRocket)
            {
                ufoBossRocket.Reset();
                ufoBossRocket.IsAnimating = true;
                ufoBossRocket.SetPopping();

                ufoBossRocket.Reposition(
                    UfoBoss: ufoBoss);

                SyncDropShadow(ufoBossRocket);
                SetUfoBossRocketDirection(source: ufoBoss, rocket: ufoBossRocket, rocketTarget: _player);

                LoggerExtensions.Log("UfoBoss Bomb dropped.");

                return true;
            }

            return false;
        }

        private bool AnimateUfoBossRocket(Construct ufoBossRocket)
        {
            UfoBossRocket ufoBossRocket1 = ufoBossRocket as UfoBossRocket;

            var speed = (_scene_game.Speed + ufoBossRocket.SpeedOffset);

            if (ufoBossRocket1.AwaitMoveDownLeft)
            {
                ufoBossRocket1.MoveDownLeft(speed);
            }
            else if (ufoBossRocket1.AwaitMoveUpRight)
            {
                ufoBossRocket1.MoveUpRight(speed);
            }
            else if (ufoBossRocket1.AwaitMoveUpLeft)
            {
                ufoBossRocket1.MoveUpLeft(speed);
            }
            else if (ufoBossRocket1.AwaitMoveDownRight)
            {
                ufoBossRocket1.MoveDownRight(speed);
            }

            if (ufoBossRocket1.IsBlasting)
            {
                ufoBossRocket.Expand();
                ufoBossRocket.Fade(0.02);
            }
            else
            {
                ufoBossRocket.Pop();
                ufoBossRocket1.Hover();

                if (_scene_game.SceneState == SceneState.GAME_RUNNING)
                {
                    if (ufoBossRocket.GetCloseHitBox().IntersectsWith(_player.GetCloseHitBox()))
                    {
                        ufoBossRocket1.SetBlast();
                        LoosePlayerHealth();
                    }

                    if (ufoBossRocket1.AutoBlast())
                        ufoBossRocket1.SetBlast();
                }
            }

            return true;
        }

        private bool RecycleUfoBossRocket(Construct ufoBossRocket)
        {
            //var hitbox = bomb.GetHitBox();

            // if bomb is blasted and faed or goes out of scene bounds
            if (ufoBossRocket.IsFadingComplete /*|| hitbox.Left > Constants.DEFAULT_SCENE_WIDTH || hitbox.Right < 0 || hitbox.Top < 0 || hitbox.Top > Constants.DEFAULT_SCENE_HEIGHT*/)
            {
                ufoBossRocket.IsAnimating = false;

                ufoBossRocket.SetPosition(
                    left: -3000,
                    top: -3000);

                return true;
            }

            return false;
        }

        #endregion

        #region VehicleBossRocket

        private bool SpawnVehicleBossRockets()
        {
            for (int i = 0; i < 4; i++)
            {
                VehicleBossRocket vehicleBossRocket = new(
                    animateAction: AnimateVehicleBossRocket,
                    recycleAction: RecycleVehicleBossRocket);

                vehicleBossRocket.SetPosition(
                    left: -3000,
                    top: -3000,
                    z: 7);

                _scene_game.AddToScene(vehicleBossRocket);
            }

            return true;
        }

        private bool GenerateVehicleBossRocket()
        {
            if (_scene_game.SceneState == SceneState.GAME_RUNNING &&
                _scene_game.Children.OfType<VehicleBoss>().FirstOrDefault(x => x.IsAnimating && x.IsAttacking) is VehicleBoss vehicleBoss &&
                _scene_game.Children.OfType<VehicleBossRocket>().FirstOrDefault(x => x.IsAnimating == false) is VehicleBossRocket vehicleBossRocket)
            {
                vehicleBossRocket.Reset();
                vehicleBossRocket.IsAnimating = true;
                vehicleBossRocket.SetPopping();

                vehicleBossRocket.Reposition(vehicleBoss: vehicleBoss);
                vehicleBossRocket.AwaitMoveUpRight = true;

                LoggerExtensions.Log("VehicleBoss Bomb dropped.");

                return true;
            }

            return false;
        }

        private bool AnimateVehicleBossRocket(Construct vehicleBossRocket)
        {
            VehicleBossRocket vehicleBossRocket1 = vehicleBossRocket as VehicleBossRocket;

            var speed = (_scene_game.Speed + vehicleBossRocket.SpeedOffset);

            if (vehicleBossRocket1.AwaitMoveUpRight)
            {
                vehicleBossRocket1.MoveUpRight(speed);
            }

            if (vehicleBossRocket1.IsBlasting)
            {
                vehicleBossRocket.Expand();
                vehicleBossRocket.Fade(0.02);
            }
            else
            {
                vehicleBossRocket.Pop();
                vehicleBossRocket1.DillyDally();

                if (_scene_game.SceneState == SceneState.GAME_RUNNING)
                {
                    if (vehicleBossRocket.GetCloseHitBox().IntersectsWith(_player.GetCloseHitBox()))
                    {
                        vehicleBossRocket1.SetBlast();
                        LoosePlayerHealth();
                    }

                    if (vehicleBossRocket1.AutoBlast())
                        vehicleBossRocket1.SetBlast();
                }
            }

            return true;
        }

        private bool RecycleVehicleBossRocket(Construct vehicleBossRocket)
        {
            //var hitbox = bomb.GetHitBox();

            // if bomb is blasted and faed or goes out of scene bounds
            if (vehicleBossRocket.IsFadingComplete /*|| hitbox.Left > Constants.DEFAULT_SCENE_WIDTH || hitbox.Right < 0 || hitbox.Top < 0 || hitbox.Top > Constants.DEFAULT_SCENE_HEIGHT*/)
            {
                vehicleBossRocket.IsAnimating = false;

                vehicleBossRocket.SetPosition(
                    left: -3000,
                    top: -3000);

                return true;
            }

            return false;
        }

        #endregion

        #region PlayerRocketSeeking

        private bool SpawnPlayerRocketSeekings()
        {
            for (int i = 0; i < 3; i++)
            {
                PlayerRocketSeeking playerRocketSeeking = new(
                    animateAction: AnimatePlayerRocketSeeking,
                    recycleAction: RecyclePlayerRocketSeeking);

                playerRocketSeeking.SetPosition(
                    left: -3000,
                    top: -3000,
                    z: 7);

                _scene_game.AddToScene(playerRocketSeeking);

                SpawnDropShadow(source: playerRocketSeeking);
            }

            return true;
        }

        private bool GeneratePlayerRocketSeeking()
        {
            // generate a seeking bomb if one is not in scene

            if (_scene_game.SceneState == SceneState.GAME_RUNNING && !_scene_game.IsSlowMotionActivated &&
                _scene_game.Children.OfType<PlayerRocketSeeking>().FirstOrDefault(x => x.IsAnimating == false) is PlayerRocketSeeking playerRocketSeeking)
            {
                _player.SetAttackStance();

                playerRocketSeeking.Reset();
                playerRocketSeeking.IsAnimating = true;
                playerRocketSeeking.SetPopping();

                playerRocketSeeking.Reposition(
                    player: _player);

                SyncDropShadow(playerRocketSeeking);

                if (_powerUp_health_bar.HasHealth && (PowerUpType)_powerUp_health_bar.Tag == PowerUpType.SEEKING_BALLS)
                    DepletePowerUp();

                LoggerExtensions.Log("Player Seeking Bomb dropped.");

                return true;
            }

            return false;
        }

        private bool AnimatePlayerRocketSeeking(Construct playerRocketSeeking)
        {
            PlayerRocketSeeking playerRocketSeeking1 = playerRocketSeeking as PlayerRocketSeeking;

            if (playerRocketSeeking1.IsBlasting)
            {
                var speed = _scene_game.Speed + playerRocketSeeking.SpeedOffset;

                MoveConstructBottomRight(construct: playerRocketSeeking1, speed: speed);

                playerRocketSeeking.Expand();
                playerRocketSeeking.Fade(0.02);
            }
            else
            {
                playerRocketSeeking.Pop();
                playerRocketSeeking.Rotate(rotationSpeed: 3.5);

                if (_scene_game.SceneState == SceneState.GAME_RUNNING)
                {
                    if (_scene_game.Children.OfType<UfoBossRocketSeeking>().FirstOrDefault(x => x.IsAnimating && !x.IsBlasting) is UfoBossRocketSeeking UfoBossRocketSeeking) // target UfoBoss bomb seeking
                    {
                        playerRocketSeeking1.Seek(UfoBossRocketSeeking.GetCloseHitBox());

                        if (playerRocketSeeking1.GetCloseHitBox().IntersectsWith(UfoBossRocketSeeking.GetCloseHitBox()))
                        {
                            playerRocketSeeking1.SetBlast();
                            UfoBossRocketSeeking.SetBlast();
                        }
                    }
                    else if (_scene_game.Children.OfType<UfoBoss>().FirstOrDefault(x => x.IsAnimating && x.IsAttacking) is UfoBoss UfoBoss) // target UfoBoss
                    {
                        playerRocketSeeking1.Seek(UfoBoss.GetCloseHitBox());

                        if (playerRocketSeeking1.GetCloseHitBox().IntersectsWith(UfoBoss.GetCloseHitBox()))
                        {
                            playerRocketSeeking1.SetBlast();
                            LooseUfoBossHealth(UfoBoss);
                        }
                    }
                    else if (_scene_game.Children.OfType<UfoEnemy>().FirstOrDefault(x => x.IsAnimating && !x.IsFadingComplete) is UfoEnemy enemy) // target enemy
                    {
                        playerRocketSeeking1.Seek(enemy.GetCloseHitBox());

                        if (playerRocketSeeking1.GetCloseHitBox().IntersectsWith(enemy.GetCloseHitBox()))
                        {
                            playerRocketSeeking1.SetBlast();
                            LooseUfoEnemyHealth(enemy);
                        }
                    }

                    if (playerRocketSeeking1.RunOutOfTimeToBlast())
                        playerRocketSeeking1.SetBlast();
                }
            }

            return true;
        }

        private bool RecyclePlayerRocketSeeking(Construct playerRocketSeeking)
        {
            var hitbox = playerRocketSeeking.GetHitBox();

            // if bomb is blasted and faed or goes out of scene bounds
            if (playerRocketSeeking.IsFadingComplete || hitbox.Left > Constants.DEFAULT_SCENE_WIDTH || hitbox.Right < 0 || hitbox.Top < 0 || hitbox.Bottom > Constants.DEFAULT_SCENE_HEIGHT)
            {
                playerRocketSeeking.IsAnimating = false;

                playerRocketSeeking.SetPosition(
                    left: -3000,
                    top: -3000);

                return true;
            }

            return false;
        }

        private void DepletePowerUp()
        {
            // use up the power up
            if (_powerUp_health_bar.HasHealth)
                _powerUp_health_bar.SetValue(_powerUp_health_bar.GetValue() - 1);
        }

        #endregion

        #region UfoBossRocketSeeking

        private bool SpawnUfoBossRocketSeekings()
        {
            for (int i = 0; i < 2; i++)
            {
                UfoBossRocketSeeking ufoBossRocketSeeking = new(
                    animateAction: AnimateUfoBossRocketSeeking,
                    recycleAction: RecycleUfoBossRocketSeeking);

                ufoBossRocketSeeking.SetPosition(
                    left: -3000,
                    top: -3000,
                    z: 7);

                _scene_game.AddToScene(ufoBossRocketSeeking);

                SpawnDropShadow(source: ufoBossRocketSeeking);
            }

            return true;
        }

        private bool GenerateUfoBossRocketSeeking()
        {
            // generate a seeking bomb if one is not in scene
            if (_scene_game.SceneState == SceneState.GAME_RUNNING &&
                _scene_game.Children.OfType<UfoBoss>().FirstOrDefault(x => x.IsAnimating && x.IsAttacking) is UfoBoss ufoBoss &&
                !_scene_game.Children.OfType<UfoBossRocketSeeking>().Any(x => x.IsAnimating) &&
                _scene_game.Children.OfType<UfoBossRocketSeeking>().FirstOrDefault(x => x.IsAnimating == false) is UfoBossRocketSeeking ufoBossRocketSeeking)
            {
                ufoBossRocketSeeking.Reset();
                ufoBossRocketSeeking.IsAnimating = true;
                ufoBossRocketSeeking.SetPopping();

                ufoBossRocketSeeking.Reposition(
                    UfoBoss: ufoBoss);

                SyncDropShadow(ufoBossRocketSeeking);

                LoggerExtensions.Log("UfoBoss Seeking Bomb dropped.");

                return true;
            }

            return false;
        }

        private bool AnimateUfoBossRocketSeeking(Construct ufoBossRocketSeeking)
        {
            UfoBossRocketSeeking UfoBossRocketSeeking1 = ufoBossRocketSeeking as UfoBossRocketSeeking;

            var speed = (_scene_game.Speed + ufoBossRocketSeeking.SpeedOffset);

            if (UfoBossRocketSeeking1.IsBlasting)
            {
                MoveConstructBottomRight(construct: UfoBossRocketSeeking1, speed: speed);

                ufoBossRocketSeeking.Expand();
                ufoBossRocketSeeking.Fade(0.02);
            }
            else
            {
                ufoBossRocketSeeking.Pop();

                if (_scene_game.SceneState == SceneState.GAME_RUNNING)
                {
                    if (_scene_game.Children.OfType<UfoBoss>().Any(x => x.IsAnimating && x.IsAttacking))
                    {
                        UfoBossRocketSeeking1.Seek(_player.GetCloseHitBox());

                        if (UfoBossRocketSeeking1.GetCloseHitBox().IntersectsWith(_player.GetCloseHitBox()))
                        {
                            UfoBossRocketSeeking1.SetBlast();
                            LoosePlayerHealth();
                        }
                        else
                        {
                            if (UfoBossRocketSeeking1.RunOutOfTimeToBlast())
                                UfoBossRocketSeeking1.SetBlast();
                        }
                    }
                    else
                    {
                        UfoBossRocketSeeking1.SetBlast();
                    }
                }
            }

            return true;
        }

        private bool RecycleUfoBossRocketSeeking(Construct ufoBossRocketSeeking)
        {
            var hitbox = ufoBossRocketSeeking.GetHitBox();

            // if bomb is blasted and faed or goes out of scene bounds
            if (ufoBossRocketSeeking.IsFadingComplete || hitbox.Left > Constants.DEFAULT_SCENE_WIDTH || hitbox.Right < 0 || hitbox.Top < 0 || hitbox.Bottom > Constants.DEFAULT_SCENE_HEIGHT)
            {
                ufoBossRocketSeeking.IsAnimating = false;

                ufoBossRocketSeeking.SetPosition(
                    left: -3000,
                    top: -3000);

                return true;
            }

            return false;
        }

        #endregion

        #region Rocket

        private void SetPlayerRocketDirection(Construct source, AnimableConstruct rocket, Construct rocketTarget)
        {
            // rocket target is on the bottom right side of the UfoBoss
            if (rocketTarget.GetTop() > source.GetTop() && rocketTarget.GetLeft() > source.GetLeft())
            {
                rocket.AwaitMoveDownRight = true;
                rocket.SetRotation(33);
            }
            // rocket target is on the bottom left side of the UfoBoss
            else if (rocketTarget.GetTop() > source.GetTop() && rocketTarget.GetLeft() < source.GetLeft())
            {
                rocket.AwaitMoveDownLeft = true;
                rocket.SetRotation(-213);
            }
            // if rocket target is on the top left side of the UfoBoss
            else if (rocketTarget.GetTop() < source.GetTop() && rocketTarget.GetLeft() < source.GetLeft())
            {
                rocket.AwaitMoveUpLeft = true;
                rocket.SetRotation(213);
            }
            // if rocket target is on the top right side of the UfoBoss
            else if (rocketTarget.GetTop() < source.GetTop() && rocketTarget.GetLeft() > source.GetLeft())
            {
                rocket.AwaitMoveUpRight = true;
                rocket.SetRotation(-33);
            }
            else
            {
                rocket.AwaitMoveUpLeft = true;
                rocket.SetRotation(213);
            }
        }

        private void SetUfoBossRocketDirection(Construct source, AnimableConstruct rocket, Construct rocketTarget)
        {
            // rocket target is on the bottom right side of the UfoBoss
            if (rocketTarget.GetTop() > source.GetTop() && rocketTarget.GetLeft() > source.GetLeft())
            {
                rocket.AwaitMoveDownRight = true;
                rocket.SetRotation(33);
            }
            // rocket target is on the bottom left side of the UfoBoss
            else if (rocketTarget.GetTop() > source.GetTop() && rocketTarget.GetLeft() < source.GetLeft())
            {
                rocket.AwaitMoveDownLeft = true;
                rocket.SetRotation(-213);
            }
            // if rocket target is on the top left side of the UfoBoss
            else if (rocketTarget.GetTop() < source.GetTop() && rocketTarget.GetLeft() < source.GetLeft())
            {
                rocket.AwaitMoveUpLeft = true;
                rocket.SetRotation(213);
            }
            // if rocket target is on the top right side of the UfoBoss
            else if (rocketTarget.GetTop() < source.GetTop() && rocketTarget.GetLeft() > source.GetLeft())
            {
                rocket.AwaitMoveUpRight = true;
                rocket.SetRotation(-33);
            }
            else
            {
                rocket.AwaitMoveDownRight = true;
                rocket.SetRotation(33);
            }
        }

        #endregion

        #region DropShadow

        private bool SpawnDropShadow(Construct source)
        {
            DropShadow dropShadow = new(
                animateAction: AnimateDropShadow,
                recycleAction: RecycleDropShadow);

            _scene_game.AddToScene(dropShadow);

            dropShadow.SetParent(construct: source);
            dropShadow.Move();
            dropShadow.SetZ(source.GetZ() - 1);

            return true;
        }

        private bool AnimateDropShadow(Construct construct)
        {
            DropShadow dropShadow = construct as DropShadow;
            dropShadow.SyncWidth();
            dropShadow.Move();

            return true;
        }

        private bool RecycleDropShadow(Construct dropShadow)
        {
            DropShadow dropShadow1 = dropShadow as DropShadow;

            if (!dropShadow1.ParentConstruct.IsAnimating)
            {
                dropShadow.IsAnimating = false;

                dropShadow.SetPosition(
                    left: -3000,
                    top: -3000);

                return true;
            }

            return false;
        }

        private void SyncDropShadow(Construct source)
        {
            if (_scene_game.Children.OfType<DropShadow>().FirstOrDefault(x => x.Id == source.Id) is DropShadow dropShadow)
            {
                dropShadow.ParentConstructSpeed = _scene_game.Speed + source.SpeedOffset;
                dropShadow.IsAnimating = true;

                dropShadow.SetZ(source.GetZ() - 2);

                dropShadow.Reset();
            }
        }

        #endregion

        #region HealthPickup

        private bool SpawnHealthPickups()
        {
            for (int i = 0; i < 3; i++)
            {
                HealthPickup healthPickup = new(
                    animateAction: AnimateHealthPickup,
                    recycleAction: RecycleHealthPickup);

                healthPickup.SetPosition(
                    left: -3000,
                    top: -3000,
                    z: 6);

                _scene_game.AddToScene(healthPickup);
            }

            return true;
        }

        private bool GenerateHealthPickups()
        {
            if (_scene_game.SceneState == SceneState.GAME_RUNNING && HealthPickup.ShouldGenerate(_player.Health) &&
                _scene_game.Children.OfType<HealthPickup>().FirstOrDefault(x => x.IsAnimating == false) is HealthPickup healthPickup)
            {
                healthPickup.IsAnimating = true;
                healthPickup.Reset();

                var topOrLeft = _random.Next(2);

                var lane = _random.Next(2);

                switch (topOrLeft)
                {
                    case 0:
                        {
                            var xLaneWidth = Constants.DEFAULT_SCENE_WIDTH / 4;
                            healthPickup.SetPosition(
                                left: _random.Next(Convert.ToInt32(xLaneWidth - healthPickup.Width)),
                                top: healthPickup.Height * -1);
                        }
                        break;
                    case 1:
                        {
                            var yLaneWidth = (Constants.DEFAULT_SCENE_HEIGHT / 2) / 2;
                            healthPickup.SetPosition(
                                left: healthPickup.Width * -1,
                                top: _random.Next(Convert.ToInt32(yLaneWidth)));
                        }
                        break;
                    default:
                        break;
                }

                SyncDropShadow(healthPickup);

                return true;
            }

            return false;
        }

        private bool AnimateHealthPickup(Construct healthPickup)
        {
            var speed = _scene_game.Speed + healthPickup.SpeedOffset;

            HealthPickup healthPickup1 = healthPickup as HealthPickup;

            if (healthPickup1.IsPickedUp)
            {
                healthPickup1.Shrink();
            }
            else
            {
                MoveConstructBottomRight(construct: healthPickup, speed: speed);

                if (_scene_game.SceneState == SceneState.GAME_RUNNING)
                {
                    var hitbox = healthPickup.GetCloseHitBox();

                    if (_player.GetCloseHitBox().IntersectsWith(hitbox))
                    {
                        healthPickup1.PickedUp();

                        _player.GainHealth();
                        _player_health_bar.SetValue(_player.Health);
                    }
                }
            }

            return true;
        }

        private bool RecycleHealthPickup(Construct healthPickup)
        {
            var hitBox = healthPickup.GetHitBox();

            if (hitBox.Top > Constants.DEFAULT_SCENE_HEIGHT || hitBox.Left > Constants.DEFAULT_SCENE_WIDTH || healthPickup.IsShrinkingComplete)
            {
                healthPickup.SetPosition(
                    left: -3000,
                    top: -3000);

                healthPickup.IsAnimating = false;
            }

            return true;
        }

        #endregion

        #region PowerUpPickup

        private bool SpawnPowerUpPickups()
        {
            for (int i = 0; i < 3; i++)
            {
                PowerUpPickup powerUpPickup = new(
                    animateAction: AnimatePowerUpPickup,
                    recycleAction: RecyclePowerUpPickup);

                powerUpPickup.SetPosition(
                    left: -3000,
                    top: -3000,
                    z: 6);

                _scene_game.AddToScene(powerUpPickup);
            }

            return true;
        }

        private bool GeneratePowerUpPickups()
        {
            if (_scene_game.SceneState == SceneState.GAME_RUNNING)
            {
                if ((UfoBossExists() || UfoEnemyExists()) && !_powerUp_health_bar.HasHealth) // if a UfoBoss or enemy exists and currently player has no other power up
                {
                    if (_scene_game.Children.OfType<PowerUpPickup>().FirstOrDefault(x => x.IsAnimating == false) is PowerUpPickup powerUpPickup)
                    {
                        powerUpPickup.IsAnimating = true;
                        powerUpPickup.Reset();

                        var topOrLeft = _random.Next(2);

                        var lane = _random.Next(2);

                        switch (topOrLeft)
                        {
                            case 0:
                                {
                                    var xLaneWidth = Constants.DEFAULT_SCENE_WIDTH / 4;
                                    powerUpPickup.SetPosition(
                                        left: _random.Next(Convert.ToInt32(xLaneWidth - powerUpPickup.Width)),
                                        top: powerUpPickup.Height * -1);
                                }
                                break;
                            case 1:
                                {
                                    var yLaneWidth = (Constants.DEFAULT_SCENE_HEIGHT / 2) / 2;
                                    powerUpPickup.SetPosition(
                                        left: powerUpPickup.Width * -1,
                                        top: _random.Next(Convert.ToInt32(yLaneWidth)));
                                }
                                break;
                            default:
                                break;
                        }

                        SyncDropShadow(powerUpPickup);

                        return true;
                    }
                }
            }

            return false;
        }

        private bool AnimatePowerUpPickup(Construct powerUpPickup)
        {
            var speed = _scene_game.Speed + powerUpPickup.SpeedOffset;

            PowerUpPickup powerUpPickup1 = powerUpPickup as PowerUpPickup;

            if (powerUpPickup1.IsPickedUp)
            {
                powerUpPickup1.Shrink();
            }
            else
            {
                MoveConstructBottomRight(construct: powerUpPickup, speed: speed);

                if (_scene_game.SceneState == SceneState.GAME_RUNNING)
                {
                    var hitbox = powerUpPickup.GetCloseHitBox();

                    // if player picks up seeking bomb pickup
                    if (_player.GetCloseHitBox().IntersectsWith(hitbox))
                    {
                        powerUpPickup1.PickedUp();

                        // if seeking balls powerup, allow using a burst of 3 seeking bombs 3 times
                        _powerUp_health_bar.Tag = powerUpPickup1.PowerUpType;
                        _powerUp_health_bar.SetMaxiumHealth(9);
                        _powerUp_health_bar.SetValue(9);
                        _powerUp_health_bar.SetIcon(powerUpPickup1.GetContentUri());
                        _powerUp_health_bar.SetBarForegroundColor(color: Colors.Green);
                    }
                }
            }

            return true;
        }

        private bool RecyclePowerUpPickup(Construct powerUpPickup)
        {
            var hitBox = powerUpPickup.GetHitBox();

            if (hitBox.Top > Constants.DEFAULT_SCENE_HEIGHT || hitBox.Left > Constants.DEFAULT_SCENE_WIDTH || powerUpPickup.IsShrinkingComplete)
            {
                powerUpPickup.IsAnimating = false;

                powerUpPickup.SetPosition(
                    left: -3000,
                    top: -3000);
            }

            return true;
        }

        #endregion

        #region Construct

        private void MoveConstructBottomRight(Construct construct, double speed)
        {
            construct.SetLeft(construct.GetLeft() + speed);
            construct.SetTop(construct.GetTop() + speed * construct.IsometricDisplacement);
        }

        #endregion

        #region Controller

        private void SetController()
        {
            _game_controller.SetScene(_scene_game);
            _game_controller.SetPauseAction(PauseGame);
            _game_controller.SetGyrometer();
        }

        private void UnsetController()
        {
            _game_controller.SetScene(null);
            _game_controller.UnsetGyrometer();
        }

        private void ToggleHudVisibility(Visibility visibility)
        {
            _game_controller.Visibility = visibility;
            _game_score_bar.Visibility = visibility;
            _health_bars.Visibility = visibility;
        }

        #endregion

        #region Scene

        private void SetScene()
        {
            _scene_game.Clear();
            _scene_main_menu.Clear();

            _powerUp_health_bar.Reset();
            _ufo_boss_health_bar.Reset();
            _game_score_bar.Reset();

            AddGeneratorsToScene();

            if (ScreenExtensions.GetScreenOrienation() == ScreenExtensions.RequiredScreenOrientation)
                _scene_game.Play();

            _scene_main_menu.Play();
        }

        private void AddGeneratorsToScene()
        {
            _scene_game.AddToScene(

            // add road marks
            new Generator(
                generationDelay: 30,
                generationAction: GenerateRoadMark,
                startUpAction: SpawnRoadMarks),

            new Generator(
                generationDelay: 180,
                generationAction: GenerateRoadSideBillboardTop,
                startUpAction: SpawnRoadSideBillboards),

            new Generator(
                generationDelay: 90,
                generationAction: GenerateRoadSideLampTop,
                startUpAction: SpawnRoadSideLamps),

            new Generator(
                generationDelay: 90,
                generationAction: GenerateRoadSideLampBottom,
                startUpAction: SpawnRoadSideLamps),

            // add road side walks
            new Generator(
                generationDelay: 27,
                generationAction: GenerateRoadSideWalkTop,
                startUpAction: SpawnRoadSideWalks),

            new Generator(
                generationDelay: 27,
                generationAction: GenerateRoadSideWalkBottom,
                startUpAction: SpawnRoadSideWalks),

            //new Generator(
            //    generationDelay: 29,
            //    generationAction: GenerateRoadSideWalkSlopeTop,
            //    startUpAction: SpawnRoadSideWalkSlopes),

            //new Generator(
            //    generationDelay: 29,
            //    generationAction: GenerateRoadSideWalkSlopeBottom,
            //    startUpAction: SpawnRoadSideWalkSlopes),

            // then add the top trees
            new Generator(
                generationDelay: 30,
                generationAction: GenerateRoadSideTreeTop,
                startUpAction: SpawnRoadSideTrees),

            // then add the bottom trees which will appear forward in z wrt to the vehicles
            new Generator(
                generationDelay: 30,
                generationAction: GenerateRoadSideTreeBottom,
                startUpAction: SpawnRoadSideTrees),

            // then add the top RoadSideHedges
            new Generator(
                generationDelay: 16,
                generationAction: GenerateRoadSideHedgeTop,
                startUpAction: SpawnRoadSideHedges),

            // then add the bottom RoadSideHedges which will appear forward in z wrt to the vehicles
            new Generator(
                generationDelay: 16,
                generationAction: GenerateRoadSideHedgeBottom,
                startUpAction: SpawnRoadSideHedges),

            // then add the vehicles which will appear forward in z wrt the top trees
            new Generator(
                generationDelay: 100,
                generationAction: GenerateVehicle,
                startUpAction: SpawnVehicles),

            // add the honks which will appear forward in z wrt to everything on the road
            new Generator(
                generationDelay: 0,
                generationAction: () => { return true; },
                startUpAction: SpawnHonks),

            // add the player in scene which will appear forward in z wrt to all else
            new Generator(
                generationDelay: 0,
                generationAction: () => { return true; },
                startUpAction: SpawnPlayerBalloon),

            new Generator(
                generationDelay: 0,
                generationAction: () => { return true; },
                startUpAction: SpawnPlayerRockets),

            new Generator(
                generationDelay: 0,
                generationAction: () => { return true; },
                startUpAction: SpawnPlayerFireCrackers),

            // add the clouds which are above the player z
            new Generator(
                generationDelay: 400,
                generationAction: GenerateCloud,
                startUpAction: SpawnClouds,
                randomizeGenerationDelay: true),

            new Generator(
                generationDelay: 100,
                generationAction: GenerateUfoBoss,
                startUpAction: SpawnUfoBosses),

            new Generator(
                generationDelay: 10,
                generationAction: GenerateVehicleBoss,
                startUpAction: SpawnVehicleBosses),

            new Generator(
                generationDelay: 50,
                generationAction: GenerateVehicleBossRocket,
                startUpAction: SpawnVehicleBossRockets,
                randomizeGenerationDelay: true),

            new Generator(
                generationDelay: 40,
                generationAction: GenerateUfoBossRocket,
                startUpAction: SpawnUfoBossRockets,
                randomizeGenerationDelay: true),

            new Generator(
                generationDelay: 200,
                generationAction: GenerateUfoBossRocketSeeking,
                startUpAction: SpawnUfoBossRocketSeekings,
                randomizeGenerationDelay: true),

            new Generator(
                generationDelay: 0,
                generationAction: () => { return true; },
                startUpAction: SpawnPlayerRocketSeekings),

            new Generator(
                generationDelay: 180,
                generationAction: GenerateUfoEnemy,
                startUpAction: SpawnUfoEnemys,
                randomizeGenerationDelay: true),

                new Generator(
                generationDelay: 0,
                generationAction: () => { return true; },
                startUpAction: SpawnUfoEnemyRockets),

                new Generator(
                generationDelay: 500,
                generationAction: GenerateHealthPickups,
                startUpAction: SpawnHealthPickups,
                randomizeGenerationDelay: true),

            new Generator(
                generationDelay: 600,
                generationAction: GeneratePowerUpPickups,
                startUpAction: SpawnPowerUpPickups,
                randomizeGenerationDelay: true)
                );

            _scene_main_menu.AddToScene(

            new Generator(
                generationDelay: 0,
                generationAction: () => { return true; },
                startUpAction: SpawnInterimScreen),

            new Generator(
                generationDelay: 0,
                generationAction: () => { return true; },
                startUpAction: SpawnTitleScreen),

            new Generator(
                generationDelay: 0,
                generationAction: () => { return true; },
                startUpAction: SpawnPlayerSelectionScreen),

            new Generator(
                generationDelay: 0,
                generationAction: () => { return true; },
                startUpAction: SpawnDisplayOrientationChangeScreen)
                );
        }

        private void SetScreenScaling()
        {
            var scaling = ScreenExtensions.GetScreenSpaceScaling();

            LoggerExtensions.Log($"ScreenSpaceScaling: {scaling}");

            // resize the game scene
            _scene_game.Width = ScreenExtensions.Width;
            _scene_game.Height = ScreenExtensions.Height;

            // resize the main menu
            _scene_main_menu.Width = ScreenExtensions.Width;
            _scene_main_menu.Height = ScreenExtensions.Height;


            // scale the scenes
            _scene_game.SetScaleTransform(scaling);
            _scene_main_menu.SetScaleTransform(scaling);
        }

        #endregion

        #endregion

        #region Events

        private async void HonkBomberPage_Loaded(object sender, RoutedEventArgs e)
        {
            ScreenExtensions.DisplayInformation.OrientationChanged += DisplayInformation_OrientationChanged;
            ScreenExtensions.RequiredScreenOrientation = DisplayOrientations.Landscape;

            // set display orientation to required orientation
            if (ScreenExtensions.GetScreenOrienation() != ScreenExtensions.RequiredScreenOrientation)
                ScreenExtensions.SetScreenOrientation(ScreenExtensions.RequiredScreenOrientation);

            SetController();
            SetScene();

            SizeChanged += HonkBomberPage_SizeChanged;

            if (ScreenExtensions.GetScreenOrienation() == ScreenExtensions.RequiredScreenOrientation)
            {
                ScreenExtensions.EnterFullScreen(true);

                await Task.Delay(1500);

                GenerateTitleScreen("Honk Trooper");
                _audio_stub.Play(SoundType.GAME_BACKGROUND_MUSIC);
            }
            else
            {
                GenerateDisplayOrientationChangeScreen();
            }
        }

        private void HonkBomberPage_Unloaded(object sender, RoutedEventArgs e)
        {
            SizeChanged -= HonkBomberPage_SizeChanged;
            ScreenExtensions.DisplayInformation.OrientationChanged -= DisplayInformation_OrientationChanged;
            UnsetController();
        }

        private void HonkBomberPage_SizeChanged(object sender, SizeChangedEventArgs args)
        {
            ScreenExtensions.Width = args.NewSize.Width <= Constants.DEFAULT_SCENE_WIDTH ? args.NewSize.Width : Constants.DEFAULT_SCENE_WIDTH;
            ScreenExtensions.Height = args.NewSize.Height <= Constants.DEFAULT_SCENE_HEIGHT ? args.NewSize.Height : Constants.DEFAULT_SCENE_HEIGHT;

            SetScreenScaling();

            if (_scene_game.SceneState == SceneState.GAME_RUNNING)
            {
                _player.Reposition();
                SyncDropShadow(_player);
            }

            RepositionHoveringTitleScreens();
            LoggerExtensions.Log($"{ScreenExtensions.Width} x {ScreenExtensions.Height}");
        }

        private void DisplayInformation_OrientationChanged(DisplayInformation sender, object args)
        {
            if (_scene_game.SceneState == SceneState.GAME_RUNNING) // if screen orientation is changed while game is running, pause the game
            {
                if (_scene_game.IsAnimating)
                    PauseGame();
            }
            else
            {
                ScreenExtensions.EnterFullScreen(true);

                if (ScreenExtensions.GetScreenOrienation() == ScreenExtensions.RequiredScreenOrientation)
                {
                    if (_scene_main_menu.Children.OfType<DisplayOrientationChangeScreen>().FirstOrDefault(x => x.IsAnimating) is DisplayOrientationChangeScreen DisplayOrientationChangeScreen)
                    {
                        RecycleDisplayOrientationChangeScreen(DisplayOrientationChangeScreen);

                        _audio_stub.Play(SoundType.GAME_BACKGROUND_MUSIC);
                        GenerateTitleScreen("Honk Trooper");

                        _scene_game.Play();
                        _scene_main_menu.Play();
                    }
                }
                else // ask to change orientation
                {
                    if (_scene_game.IsAnimating)
                        _scene_game.Pause();

                    if (!_scene_main_menu.IsAnimating)
                        _scene_main_menu.Play();

                    foreach (var hoveringTitleScreen in _scene_main_menu.Children.OfType<HoveringTitleScreen>().Where(x => x.IsAnimating))
                    {
                        hoveringTitleScreen.IsAnimating = false;
                        hoveringTitleScreen.SetPosition(left: -3000, top: -3000);
                    }

                    foreach (var construct in _scene_game.Children.OfType<Construct>())
                    {
                        construct.IsAnimating = false;
                        construct.SetPosition(left: -3000, top: -3000);
                    }

                    GenerateDisplayOrientationChangeScreen();
                }                
            }

            LoggerExtensions.Log($"{sender.CurrentOrientation}");
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            PauseGame();
        }

        #endregion
    }
}
