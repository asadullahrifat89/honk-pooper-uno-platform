﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;

namespace HonkTrooper
{
    public partial class RoadSidePatchSlope : Construct
    {
        #region Ctor

        public RoadSidePatchSlope(
            Func<Construct, bool> animateAction,
            Func<Construct, bool> recycleAction)
        {
            ConstructType = ConstructType.ROAD_SIDE_PATCH_SLOPE;

            var size = Constants.CONSTRUCT_SIZES.FirstOrDefault(x => x.ConstructType == ConstructType.ROAD_SIDE_PATCH_SLOPE);

            var width = size.Width;
            var height = size.Height;

            SetSize(width: width, height: height);

            AnimateAction = animateAction;
            RecycleAction = recycleAction;

            Background = App.Current.Resources["RoadSidePatchSlopeColor"] as SolidColorBrush;
            BorderBrush = App.Current.Resources["RoadSidePatchBorderColor"] as SolidColorBrush;
            BorderThickness = new Thickness(5);

            SetSkewY(-28);
            SetRotation(-63.5);
            CornerRadius = 2;

            IsometricDisplacement = Constants.DEFAULT_ISOMETRIC_DISPLACEMENT;
            SpeedOffset = Constants.DEFAULT_SPEED_OFFSET;
        }

        #endregion
    }
}
