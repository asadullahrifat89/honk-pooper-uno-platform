﻿using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Controls;

namespace HonkPooper
{
    public partial class Vehicle : Construct
    {
        private Random _random;

        private Uri[] _vehicle_small_uris;
        private Uri[] _vehicle_large_uris;

        public Vehicle(
            Func<Construct, bool> animateAction,
            Func<Construct, bool> recycleAction,
            double scaling)
        {
            _random = new Random();

            _vehicle_small_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.VEHICLE_SMALL).Select(x => x.Uri).ToArray();
            _vehicle_large_uris = Constants.CONSTRUCT_TEMPLATES.Where(x => x.ConstructType == ConstructType.VEHICLE_LARGE).Select(x => x.Uri).ToArray();

            WillHonk = Convert.ToBoolean(_random.Next(0, 2));

            var vehicleType = _random.Next(0, 2);

            (ConstructType ConstructType, double Height, double Width) size;
            Uri uri;

            double speedOffset = _random.Next(-4, 2);

            switch (vehicleType)
            {
                case 0:
                    {
                        size = Constants.CONSTRUCT_SIZES.FirstOrDefault(x => x.ConstructType == ConstructType.VEHICLE_SMALL);

                        var vehicles = _vehicle_small_uris;
                        uri = vehicles[_random.Next(0, vehicles.Length)];

                        ConstructType = ConstructType.VEHICLE_SMALL;

                        var width = size.Width * scaling;
                        var height = size.Height * scaling;

                        SetSize(width: width, height: height);

                        AnimateAction = animateAction;
                        RecycleAction = recycleAction;

                        var content = new Image()
                        {
                            Source = new BitmapImage(uriSource: uri)
                        };
                        SetChild(content);

                        SpeedOffset = speedOffset;
                    }
                    break;
                case 1:
                    {
                        size = Constants.CONSTRUCT_SIZES.FirstOrDefault(x => x.ConstructType == ConstructType.VEHICLE_LARGE);

                        var vehicles = _vehicle_large_uris;
                        uri = vehicles[_random.Next(0, vehicles.Length)];

                        ConstructType = ConstructType.VEHICLE_LARGE;

                        var width = size.Width * scaling;
                        var height = size.Height * scaling;

                        SetSize(width: width, height: height);

                        AnimateAction = animateAction;
                        RecycleAction = recycleAction;

                        var content = new Image()
                        {
                            Source = new BitmapImage(uriSource: uri)
                        };
                        SetChild(content);

                        SpeedOffset = speedOffset;
                    }
                    break;
                default:
                    break;
            }
        }

        public bool WillHonk { get; set; }

        public bool IsHonkBusted { get; set; }
    }
}
