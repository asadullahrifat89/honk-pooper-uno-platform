﻿using System;
using System.Linq;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Controls;

namespace HonkTrooper
{
    public partial class VehicleEnemy : VehicleBase
    {
        #region Fields

        private readonly Random _random;

        private readonly Uri[] _vehicle_small_uris;
        private readonly Uri[] _vehicle_large_uris;

        #endregion

        #region Ctor

        public VehicleEnemy(
            Func<Construct, bool> animateAction,
            Func<Construct, bool> recycleAction)
        {
            _random = new Random();

            _vehicle_small_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.VEHICLE_ENEMY_SMALL).Select(x => x.Uri).ToArray();
            _vehicle_large_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.VEHICLE_ENEMY_LARGE).Select(x => x.Uri).ToArray();

            WillHonk = Convert.ToBoolean(_random.Next(2));

            var vehicleType = _random.Next(2);

            (ConstructType ConstructType, double Height, double Width) size;
            Uri uri;

            switch (vehicleType)
            {
                case 0:
                    {
                        size = Constants.CONSTRUCT_SIZES.FirstOrDefault(x => x.ConstructType == ConstructType.VEHICLE_ENEMY_SMALL);

                        uri = ConstructExtensions.GetRandomContentUri(_vehicle_small_uris);

                        ConstructType = ConstructType.VEHICLE_ENEMY_SMALL;

                        var width = size.Width;
                        var height = size.Height;

                        SetSize(width: width, height: height);

                        AnimateAction = animateAction;
                        RecycleAction = recycleAction;

                        var content = new Image()
                        {
                            Source = new BitmapImage(uriSource: uri)
                        };
                        SetChild(content);
                    }
                    break;
                case 1:
                    {
                        size = Constants.CONSTRUCT_SIZES.FirstOrDefault(x => x.ConstructType == ConstructType.VEHICLE_ENEMY_LARGE);

                        uri = ConstructExtensions.GetRandomContentUri(_vehicle_large_uris);

                        ConstructType = ConstructType.VEHICLE_ENEMY_LARGE;

                        var width = size.Width;
                        var height = size.Height;

                        SetSize(width: width, height: height);

                        AnimateAction = animateAction;
                        RecycleAction = recycleAction;

                        var content = new Image()
                        {
                            Source = new BitmapImage(uriSource: uri)
                        };
                        SetChild(content);
                    }
                    break;
                default:
                    break;
            }

            IsometricDisplacement = Constants.DEFAULT_ISOMETRIC_DISPLACEMENT;

            if (WillHonk)
                SetHonkDelay();
        }

        #endregion     

        #region Methods

        public void Reset()
        {
            SetScaleTransform(1);

            SpeedOffset = _random.Next((int)Constants.DEFAULT_SPEED_OFFSET * -2, (int)Constants.DEFAULT_SPEED_OFFSET - 1);

            WillHonk = Convert.ToBoolean(_random.Next(2));

            if (WillHonk)
                SetHonkDelay();
        }

        public void SetBlast()
        {
            WillHonk = false;
            SetPopping();
            SpeedOffset = Constants.DEFAULT_SPEED_OFFSET - 1;
        }

        #endregion
    }
}
