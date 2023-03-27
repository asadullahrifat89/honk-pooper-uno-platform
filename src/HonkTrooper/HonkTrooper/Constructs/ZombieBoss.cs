﻿using System;
using System.Linq;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HonkTrooper
{
    public partial class ZombieBoss : AnimableConstruct
    {
        #region Fields

        private readonly Random _random;
        private readonly Uri[] _zombie_boss_idle_uris;
        private readonly Uri[] _zombie_boss_hit_uris;
        private readonly Uri[] _zombie_boss_win_uris;

        private double _changeMovementPatternDelay;

        private readonly Image _content_image;

        private double _hitStanceDelay;
        private readonly double _hitStanceDelayDefault = 1.5;

        private double _winStanceDelay;
        private readonly double _winStanceDelayDefault = 8;

        private readonly AudioStub _audioStub;

        private MovementDirection _movementDirection;

        #endregion

        #region Ctor

        public ZombieBoss(
           Func<Construct, bool> animateAction,
           Func<Construct, bool> recycleAction)
        {
            ConstructType = ConstructType.ZOMBIE_BOSS;

            _random = new Random();

            _zombie_boss_idle_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.ZOMBIE_BOSS_IDLE).Select(x => x.Uri).ToArray();
            _zombie_boss_hit_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.ZOMBIE_BOSS_HIT).Select(x => x.Uri).ToArray();
            _zombie_boss_win_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.ZOMBIE_BOSS_WIN).Select(x => x.Uri).ToArray();

            var size = Constants.CONSTRUCT_SIZES.FirstOrDefault(x => x.ConstructType == ConstructType.ZOMBIE_BOSS);

            var width = size.Width;
            var height = size.Height;

            AnimateAction = animateAction;
            RecycleAction = recycleAction;

            SetSize(width: width, height: height);

            var uri = ConstructExtensions.GetRandomContentUri(_zombie_boss_idle_uris);

            _content_image = new Image()
            {
                Source = new BitmapImage(uriSource: uri)
            };

            SetChild(_content_image);

            IsometricDisplacement = Constants.DEFAULT_ISOMETRIC_DISPLACEMENT;
            DropShadowDistance = Constants.DEFAULT_DROP_SHADOW_DISTANCE;

            _audioStub = new AudioStub(
                (SoundType.UFO_BOSS_HOVERING, 0.8, true),
                (SoundType.UFO_BOSS_ENTRY, 0.8, false),
                (SoundType.UFO_BOSS_DEAD, 1, false));
        }

        #endregion

        #region Properties

        public bool IsAttacking { get; set; }

        public double Health { get; set; }

        public bool IsDead => Health <= 0;

        public BossStance ZombieBossStance { get; set; } = BossStance.Idle;

        #endregion

        #region Methods

        public void Reset()
        {
            _audioStub.Play(SoundType.UFO_BOSS_ENTRY);

            PlaySoundLoop();

            Opacity = 1;
            Health = 100;
            IsAttacking = false;
            ZombieBossStance = BossStance.Idle;

            _movementDirection = MovementDirection.None;

            var uri = ConstructExtensions.GetRandomContentUri(_zombie_boss_idle_uris);
            _content_image.Source = new BitmapImage(uriSource: uri);

            RandomizeMovementPattern();
            SetScaleTransform(1);
        }

        private void PlaySoundLoop()
        {
            _audioStub.Play(SoundType.UFO_BOSS_HOVERING);
        }

        public void SetHitStance()
        {
            if (ZombieBossStance != BossStance.Win)
            {
                ZombieBossStance = BossStance.Hit;
                var uri = ConstructExtensions.GetRandomContentUri(_zombie_boss_hit_uris);
                _content_image.Source = new BitmapImage(uriSource: uri);
                _hitStanceDelay = _hitStanceDelayDefault;
            }
        }

        public void SetWinStance()
        {
            ZombieBossStance = BossStance.Win;
            var uri = ConstructExtensions.GetRandomContentUri(_zombie_boss_win_uris);
            _content_image.Source = new BitmapImage(uriSource: uri);
            _winStanceDelay = _winStanceDelayDefault;
        }

        private void SetIdleStance()
        {
            ZombieBossStance = BossStance.Idle;
            var uri = ConstructExtensions.GetRandomContentUri(_zombie_boss_idle_uris);
            _content_image.Source = new BitmapImage(uriSource: uri);
        }

        public void DepleteWinStance()
        {
            if (_winStanceDelay > 0)
            {
                _winStanceDelay -= 0.1;

                if (_winStanceDelay <= 0)
                {
                    SetIdleStance();
                }
            }
        }

        public void DepleteHitStance()
        {
            if (_hitStanceDelay > 0)
            {
                _hitStanceDelay -= 0.1;

                if (_hitStanceDelay <= 0)
                {
                    SetIdleStance();
                }
            }
        }

        public void LooseHealth()
        {
            Health -= 5;

            if (IsDead)
            {
                IsAttacking = false;
                StopSoundLoop();
                _audioStub.Play(SoundType.UFO_BOSS_DEAD);
            }
        }

        public void StopSoundLoop()
        {
            _audioStub.Stop(SoundType.UFO_BOSS_HOVERING);
        }

        public void Move(
           double speed,
           double sceneWidth,
           double sceneHeight)
        {
            MoveUpRightDownLeft(
                speed: speed,
                sceneWidth: sceneWidth,
                sceneHeight: sceneHeight);
        }

        private bool MoveUpRightDownLeft(double speed, double sceneWidth, double sceneHeight)
        {
            _changeMovementPatternDelay -= 0.1;

            if (_changeMovementPatternDelay < 0)
            {
                RandomizeMovementPattern();
                return true;
            }

            if (IsAttacking && _movementDirection == MovementDirection.None)
            {
                _movementDirection = MovementDirection.UpRight;
            }
            else
            {
                IsAttacking = true;
            }

            if (IsAttacking)
            {
                if (_movementDirection == MovementDirection.UpRight)
                {
                    MoveUpRight(speed);

                    if (GetTop() < 0 || GetLeft() > sceneWidth)
                    {
                        _movementDirection = MovementDirection.DownLeft;
                    }
                }
                else
                {
                    if (_movementDirection == MovementDirection.DownLeft)
                    {
                        MoveDownLeft(speed);

                        if (GetLeft() < 0 || GetBottom() > sceneHeight)
                        {
                            _movementDirection = MovementDirection.UpRight;
                        }
                    }
                }
            }

            return false;
        }

        private void RandomizeMovementPattern()
        {
            _changeMovementPatternDelay = _random.Next(40, 60);
            _movementDirection = MovementDirection.None;
            SpeedOffset = _random.Next(-1, (int)Constants.DEFAULT_SPEED_OFFSET + 1);
        }

        #endregion
    }
}