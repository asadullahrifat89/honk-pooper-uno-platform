﻿using System;
using System.Linq;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Controls;

namespace HonkTrooper
{
    public partial class VehicleBoss : VehicleBossBase
    {
        #region Fields

        private readonly Random _random;
        private readonly Uri[] _vehicle_boss_uris;


        private readonly Image _content_image;

        private MovementDirection _movementDirection;

        private double _changeMovementPatternDelay;

        #endregion

        #region Ctor

        public VehicleBoss(
            Func<Construct, bool> animateAction,
            Func<Construct, bool> recycleAction)
        {
            ConstructType = ConstructType.VEHICLE_BOSS;

            _random = new Random();

            _vehicle_boss_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.VEHICLE_ENEMY_LARGE).Select(x => x.Uri).ToArray();

            var size = Constants.CONSTRUCT_SIZES.FirstOrDefault(x => x.ConstructType == ConstructType.VEHICLE_BOSS);

            var width = size.Width;
            var height = size.Height;

            AnimateAction = animateAction;
            RecycleAction = recycleAction;

            SetSize(width: width, height: height);

            var uri = ConstructExtensions.GetRandomContentUri(_vehicle_boss_uris);

            _content_image = new Image()
            {
                Source = new BitmapImage(uriSource: uri)
            };

            SetChild(_content_image);

            IsometricDisplacement = Constants.DEFAULT_ISOMETRIC_DISPLACEMENT;
        }

        #endregion

        #region Properties      

        public VehicleBossMovementPattern MovementPattern { get; set; }

        #endregion

        #region Methods

        public new void Reset()
        {
            base.Reset();

            var uri = ConstructExtensions.GetRandomContentUri(_vehicle_boss_uris);
            _content_image.Source = new BitmapImage(uri);

            RandomizeMovementPattern();
            SetScaleTransform(1);
            SetHonkDelay();
        }

        private void RandomizeMovementPattern()
        {
            SpeedOffset = _random.Next((int)Constants.DEFAULT_SPEED_OFFSET, 4);
            MovementPattern = (VehicleBossMovementPattern)_random.Next(Enum.GetNames(typeof(VehicleBossMovementPattern)).Length);

            _changeMovementPatternDelay = _random.Next(40, 60);
            _movementDirection = MovementDirection.None;
        }

        public void Move(
           double speed,
           double sceneWidth,
           double sceneHeight)
        {
            MoveUpLeftDownRight(
                speed: speed,
                sceneWidth: sceneWidth,
                sceneHeight: sceneHeight);
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

                    if (GetBottom() < 0 || GetRight() < 0)
                    {
                        Reposition();
                        _movementDirection = MovementDirection.DownRight;
                    }
                }
                else
                {
                    if (_movementDirection == MovementDirection.DownRight)
                    {
                        MoveDownRight(speed);

                        if (GetLeft() > sceneWidth || GetTop() > sceneHeight)
                        {
                            _movementDirection = MovementDirection.UpLeft;
                        }
                    }
                }
            }

            return false;
        }

        #endregion
    }

    public enum VehicleBossMovementPattern
    {
        ISOMETRIC_SQUARE,
        UPLEFT_DOWNRIGHT,
    }
}
