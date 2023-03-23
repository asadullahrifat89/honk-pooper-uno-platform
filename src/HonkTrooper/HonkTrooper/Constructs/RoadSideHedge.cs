﻿using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Linq;

namespace HonkTrooper
{
    public partial class RoadSideHedge : Construct
    {
        #region Fields

        private readonly Image _content_image;

        #endregion

        #region Ctor

        public RoadSideHedge(
            Func<Construct, bool> animateAction,
            Func<Construct, bool> recycleAction)
        {
            var size = Constants.CONSTRUCT_SIZES.FirstOrDefault(x => x.ConstructType == ConstructType.ROAD_SIDE_HEDGE);

            ConstructType = ConstructType.ROAD_SIDE_HEDGE;

            var width = size.Width;
            var height = size.Height;

            AnimateAction = animateAction;
            RecycleAction = recycleAction;

            SetSize(width: width, height: height);

            _content_image = new Image()
            {
                Source = new BitmapImage(uriSource: Constants.CONSTRUCT_TEMPLATES.FirstOrDefault(x => x.ConstructType == ConstructType.ROAD_SIDE_HEDGE).Uri)
            };

            SetChild(_content_image);

            SpeedOffset = Constants.DEFAULT_SPEED_OFFSET;
            IsometricDisplacement = Constants.DEFAULT_ISOMETRIC_DISPLACEMENT;
            DropShadowDistance = 0;
        }

        #endregion
    }
}