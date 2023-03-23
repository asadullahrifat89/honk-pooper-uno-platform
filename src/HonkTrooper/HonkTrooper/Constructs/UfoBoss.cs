﻿using System;
using System.Linq;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HonkTrooper
{
    public partial class UfoBoss : DirectionalMovingConstruct
    {
        #region Fields

        private readonly Random _random;
        private readonly Uri[] _ufo_boss_uris;
        private readonly Uri[] _ufo_boss_hit_uris;
        private readonly Uri[] _ufo_boss_win_uris;

        private double _hoverDelay;
        private readonly double _hoverDelayDefault = 15;
        private readonly double _hoverSpeed = 0.5;

        private readonly double _grace = 7;
        private readonly double _lag = 125;

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

        public UfoBoss(
            Func<Construct, bool> animateAction,
            Func<Construct, bool> recycleAction)
        {
            _hoverDelay = _hoverDelayDefault;

            _random = new Random();

            _ufo_boss_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.UFO_BOSS).Select(x => x.Uri).ToArray();
            _ufo_boss_hit_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.UFO_BOSS_HIT).Select(x => x.Uri).ToArray();
            _ufo_boss_win_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.UFO_BOSS_WIN).Select(x => x.Uri).ToArray();

            var size = Constants.CONSTRUCT_SIZES.FirstOrDefault(x => x.ConstructType == ConstructType.UFO_BOSS);

            ConstructType = ConstructType.UFO_BOSS;

            var width = size.Width;
            var height = size.Height;

            AnimateAction = animateAction;
            RecycleAction = recycleAction;

            SetSize(width: width, height: height);

            var uri = ConstructExtensions.GetRandomContentUri(_ufo_boss_uris);

            _content_image = new Image()
            {
                Source = new BitmapImage(uriSource: uri)
            };

            SetChild(_content_image);

            IsometricDisplacement = Constants.DEFAULT_ISOMETRIC_DISPLACEMENT;
            SpeedOffset = 0;
            DropShadowDistance = Constants.DEFAULT_DROP_SHADOW_DISTANCE;

            _audioStub = new AudioStub(
                (SoundType.UFO_BOSS_HOVERING, 0.8, true),
                (SoundType.UFO_BOSS_ENTRY, 0.8, false),
                (SoundType.UFO_BOSS_DEAD, 1, false)
                );
        }

        #endregion

        #region Properties

        public bool IsAttacking { get; set; }

        public double Health { get; set; }

        public bool IsDead => Health <= 0;

        public UfoBossMovementPattern MovementPattern { get; set; }

        #endregion

        #region Methods

        public void Reset()
        {
            _audioStub.Play(SoundType.UFO_BOSS_ENTRY);

            PlaySoundLoop();

            Opacity = 1;
            Health = 100;
            IsAttacking = false;

            _movementDirection = MovementDirection.None;

            var uri = ConstructExtensions.GetRandomContentUri(_ufo_boss_uris);
            _content_image.Source = new BitmapImage(uriSource: uri);

            RandomizeMovementPattern();
            SetScaleTransform(1);
        }

        public void Hover()
        {
            if (Scene.IsSlowMotionActivated)
            {
                _hoverDelay -= 0.5;

                if (_hoverDelay >= 0)
                {
                    SetTop(GetTop() + _hoverSpeed / Constants.DEFAULT_SLOW_MOTION_REDUCTION_FACTOR);
                }
                else
                {
                    SetTop(GetTop() - _hoverSpeed / Constants.DEFAULT_SLOW_MOTION_REDUCTION_FACTOR);

                    if (_hoverDelay <= _hoverDelayDefault * -1)
                        _hoverDelay = _hoverDelayDefault;
                }
            }

            else
            {
                _hoverDelay--;

                if (_hoverDelay >= 0)
                {
                    SetTop(GetTop() + _hoverSpeed);
                }
                else
                {
                    SetTop(GetTop() - _hoverSpeed);

                    if (_hoverDelay <= _hoverDelayDefault * -1)
                        _hoverDelay = _hoverDelayDefault;
                }
            }
        }

        public void SetHitStance()
        {
            var uri = ConstructExtensions.GetRandomContentUri(_ufo_boss_hit_uris);
            _content_image.Source = new BitmapImage(uriSource: uri);
            _hitStanceDelay = _hitStanceDelayDefault;
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

        public void SetWinStance()
        {
            var uri = ConstructExtensions.GetRandomContentUri(_ufo_boss_win_uris);
            _content_image.Source = new BitmapImage(uriSource: uri);
            _winStanceDelay = _winStanceDelayDefault;
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

        public void LooseHealth()
        {
            Health -= 5;

            if (IsDead)
            {
                StopSoundLoop();
                _audioStub.Play(SoundType.UFO_BOSS_DEAD);
            }
        }

        public void PlaySoundLoop()
        {
            _audioStub.Play(SoundType.UFO_BOSS_HOVERING);
        }

        public void StopSoundLoop()
        {
            _audioStub.Stop(SoundType.UFO_BOSS_HOVERING);
        }

        public void Move(
            double speed,
            double sceneWidth,
            double sceneHeight,
            Rect playerPoint)
        {
            switch (MovementPattern)
            {
                case UfoBossMovementPattern.PLAYER_SEEKING:
                    SeekPlayer(playerPoint);
                    break;
                case UfoBossMovementPattern.ISOMETRIC_SQUARE:
                    MoveInIsometricSquares(
                        speed: speed,
                        sceneWidth: sceneWidth,
                        sceneHeight: sceneHeight);
                    break;
                case UfoBossMovementPattern.UPRIGHT_DOWNLEFT:
                    MoveUpRightDownLeft(
                        speed: speed,
                        sceneWidth: sceneWidth,
                        sceneHeight: sceneHeight);
                    break;
                case UfoBossMovementPattern.UPLEFT_DOWNRIGHT:
                    MoveUpLeftDownRight(
                        speed: speed,
                        sceneWidth: sceneWidth,
                        sceneHeight: sceneHeight);
                    break;
                case UfoBossMovementPattern.RIGHT_LEFT:
                    MoveRightLeft(
                        speed: speed,
                        sceneWidth: sceneWidth,
                        sceneHeight: sceneHeight);
                    break;
                case UfoBossMovementPattern.UP_DOWN:
                    MoveUpDown(
                        speed: speed,
                        sceneWidth: sceneWidth,
                        sceneHeight: sceneHeight);
                    break;
            }
        }

        private void SeekPlayer(Rect playerPoint)
        {
            _changeMovementPatternDelay -= 0.1;

            if (_changeMovementPatternDelay < 0)
            {
                RandomizeMovementPattern();
            }

            double left = GetLeft();
            double top = GetTop();

            double playerMiddleX = left + Width / 2;
            double playerMiddleY = top + Height / 2;

            // move up
            if (playerPoint.Y < playerMiddleY - _grace)
            {
                var distance = Math.Abs(playerPoint.Y - playerMiddleY);
                double speed = GetFlightSpeed(distance);

                SetTop(top - speed);
            }

            // move left
            if (playerPoint.X < playerMiddleX - _grace)
            {
                var distance = Math.Abs(playerPoint.X - playerMiddleX);
                double speed = GetFlightSpeed(distance);

                SetLeft(left - speed);
            }

            // move down
            if (playerPoint.Y > playerMiddleY + _grace)
            {
                var distance = Math.Abs(playerPoint.Y - playerMiddleY);
                double speed = GetFlightSpeed(distance);

                SetTop(top + speed);
            }

            // move right
            if (playerPoint.X > playerMiddleX + _grace)
            {
                var distance = Math.Abs(playerPoint.X - playerMiddleX);
                double speed = GetFlightSpeed(distance);

                SetLeft(left + speed);
            }
        }

        private bool MoveInIsometricSquares(double speed, double sceneWidth, double sceneHeight)
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

                    if (GetTop() < 0)
                    {
                        _movementDirection = MovementDirection.DownRight;
                    }
                }
                else
                {
                    if (_movementDirection == MovementDirection.DownRight)
                    {
                        MoveDownRight(speed);

                        if (GetRight() > sceneWidth || GetBottom() > sceneHeight)
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
                                _movementDirection = MovementDirection.UpLeft;
                            }
                        }
                        else
                        {
                            if (_movementDirection == MovementDirection.UpLeft)
                            {
                                MoveUpLeft(speed);

                                if (GetTop() < 0 || GetLeft() < 0)
                                {
                                    _movementDirection = MovementDirection.UpRight;
                                }
                            }
                        }
                    }
                }
            }

            return false;
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

        private bool MoveUpLeftDownRight(double speed, double sceneWidth, double sceneHeight)
        {
            _changeMovementPatternDelay -= 0.1;

            if (_changeMovementPatternDelay < 0)
            {
                RandomizeMovementPattern();
                return true;
            }

            if (IsAttacking && _movementDirection == MovementDirection.None)
            {
                _movementDirection = MovementDirection.UpLeft;
            }
            else
            {
                IsAttacking = true;
            }

            if (IsAttacking)
            {
                if (_movementDirection == MovementDirection.UpLeft)
                {
                    MoveUpLeft(speed);

                    if (GetTop() < 0 || GetLeft() < 0)
                    {
                        _movementDirection = MovementDirection.DownRight;
                    }
                }
                else
                {
                    if (_movementDirection == MovementDirection.DownRight)
                    {
                        MoveDownRight(speed);

                        if (GetRight() > sceneWidth || GetBottom() > sceneHeight)
                        {
                            _movementDirection = MovementDirection.UpLeft;
                        }
                    }
                }
            }

            return false;
        }

        private bool MoveRightLeft(double speed, double sceneWidth, double sceneHeight)
        {
            _changeMovementPatternDelay -= 0.1;

            if (_changeMovementPatternDelay < 0)
            {
                RandomizeMovementPattern();
                return true;
            }

            if (IsAttacking && _movementDirection == MovementDirection.None)
            {
                _movementDirection = MovementDirection.Right;
            }
            else
            {
                IsAttacking = true;
            }

            if (IsAttacking)
            {
                if (_movementDirection == MovementDirection.Right)
                {
                    MoveRight(speed);

                    if (GetRight() > sceneWidth)
                    {
                        _movementDirection = MovementDirection.Left;
                    }
                }
                else
                {
                    if (_movementDirection == MovementDirection.Left)
                    {
                        MoveLeft(speed);

                        if (GetLeft() < 0)
                        {
                            _movementDirection = MovementDirection.Right;
                        }
                    }
                }
            }

            return false;
        }

        private bool MoveUpDown(double speed, double sceneWidth, double sceneHeight)
        {
            _changeMovementPatternDelay -= 0.1;

            if (_changeMovementPatternDelay < 0)
            {
                RandomizeMovementPattern();
                return true;
            }

            if (IsAttacking && _movementDirection == MovementDirection.None)
            {
                _movementDirection = MovementDirection.Up;
            }
            else
            {
                IsAttacking = true;
            }

            if (IsAttacking)
            {
                if (_movementDirection == MovementDirection.Up)
                {
                    MoveUp(speed);

                    if (GetTop() < 0)
                    {
                        _movementDirection = MovementDirection.Down;
                    }
                }
                else
                {
                    if (_movementDirection == MovementDirection.Down)
                    {
                        MoveDown(speed);

                        if (GetBottom() > sceneHeight)
                        {
                            _movementDirection = MovementDirection.Up;
                        }
                    }
                }
            }

            return false;
        }

        private void SetIdleStance()
        {
            var uri = ConstructExtensions.GetRandomContentUri(_ufo_boss_uris);
            _content_image.Source = new BitmapImage(uriSource: uri);
        }

        private double GetFlightSpeed(double distance)
        {
            var flightSpeed = distance / _lag;
            return flightSpeed;

            //return flightSpeed < Constants.DEFAULT_SPEED_OFFSET - 1 
            //    ? Constants.DEFAULT_SPEED_OFFSET - 1 
            //    : flightSpeed;
        }

        private void RandomizeMovementPattern()
        {
            MovementPattern = (UfoBossMovementPattern)_random.Next(Enum.GetNames(typeof(UfoBossMovementPattern)).Length);

            _changeMovementPatternDelay = _random.Next(40, 60);            
            _movementDirection = MovementDirection.None;
        }

        #endregion
    }

    public enum UfoBossMovementPattern
    {
        PLAYER_SEEKING,
        ISOMETRIC_SQUARE,
        UPRIGHT_DOWNLEFT,
        UPLEFT_DOWNRIGHT,
        RIGHT_LEFT,
        UP_DOWN,
    }
}