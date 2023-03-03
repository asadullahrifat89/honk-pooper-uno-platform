﻿using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace HonkPooper
{
    public sealed partial class MainPage : Page
    {
        #region Fields

        private Scene _scene;
        private Random _random;

        #endregion

        #region Ctor

        public MainPage()
        {
            this.InitializeComponent();

            _scene = this.MainScene;
            _random = new Random();

            Loaded += MainPage_Loaded;
            Unloaded += MainPage_Unloaded;
        }

        #endregion

        #region Methods

        #region Vehicle

        public bool GenerateVehicleInScene()
        {
            Vehicle vehicle = new(
                animateAction: AnimateVehicle,
                recycleAction: RecycleVehicle,
                scaling: _scene.Scaling);

            _scene.AddToScene(vehicle);

            // generate top and left corner lane wise vehicles
            var topOrLeft = _random.Next(0, 2);

            var lane = _random.Next(0, 2);

            switch (topOrLeft)
            {
                case 0:
                    {
                        var xLaneWidth = _scene.Width / 4;

                        vehicle.SetPosition(
                            left: lane == 0 ? 0 : xLaneWidth - vehicle.Width * _scene.Scaling,
                            top: vehicle.Height * -1);
                    }
                    break;
                case 1:
                    {
                        var yLaneWidth = (_scene.Height / 2) / 2;

                        vehicle.SetPosition(
                            left: vehicle.Width * -1,
                            top: lane == 0 ? 0 : yLaneWidth * _scene.Scaling);
                    }
                    break;
                default:
                    break;
            }

            Console.WriteLine("Vehicle generated.");
            return true;
        }

        private bool AnimateVehicle(Construct vehicle)
        {
            var speed = _scene.Speed + vehicle.SpeedOffset;

            MoveConstruct(vehicle, speed);

            var hitHox = vehicle.GetCloseHitBox();

            // prevent overlapping

            if (_scene.Children.OfType<Vehicle>()
                .FirstOrDefault(x => x.GetCloseHitBox().IntersectsWith(hitHox)) is Construct collidingVehicle)
            {
                if (collidingVehicle.SpeedOffset < vehicle.SpeedOffset)
                {
                    collidingVehicle.SpeedOffset = vehicle.SpeedOffset;
                }
                else if (collidingVehicle.SpeedOffset > vehicle.SpeedOffset)
                {
                    vehicle.SpeedOffset = collidingVehicle.SpeedOffset;
                }
            }

            Vehicle vehicle1 = vehicle as Vehicle;

            if (hitHox.Right > 0 && hitHox.Bottom > 0 && vehicle1.Honk())
                GenerateHonkInScene(vehicle1);

            return true;
        }

        private bool RecycleVehicle(Construct vehicle)
        {
            var hitBox = vehicle.GetHitBox();

            if (hitBox.Top > _scene.Height || hitBox.Left > _scene.Width)
                _scene.DisposeFromScene(vehicle);

            return true;
        }

        #endregion

        #region RoadMark

        public bool GenerateRoadMarkInScene()
        {
            RoadMark roadMark = new(
                animateAction: AnimateRoadMark,
                recycleAction: RecycleRoadMark,
                scaling: _scene.Scaling);

            _scene.AddToScene(roadMark);

            roadMark.SetPosition(
              left: 0,
              top: 0);

            Console.WriteLine("Road Mark generated.");

            return true;
        }

        private bool AnimateRoadMark(Construct roadMark)
        {
            var speed = _scene.Speed + roadMark.SpeedOffset;
            MoveConstruct(roadMark, speed);
            return true;
        }

        private bool RecycleRoadMark(Construct roadMark)
        {
            var hitBox = roadMark.GetHitBox();

            if (hitBox.Top > _scene.Height || hitBox.Left > _scene.Width)
                _scene.DisposeFromScene(roadMark);

            return true;
        }

        #endregion

        #region Tree

        private bool GenerateTreeInSceneTop()
        {
            Construct tree = GenerateTree();

            _scene.AddToScene(tree);

            tree.SetPosition(
              left: _scene.Width / 2 - tree.Width * _scene.Scaling,
              top: tree.Height * -1);

            Console.WriteLine("Tree generated.");

            return true;
        }

        private bool GenerateTreeInSceneBottom()
        {
            Construct tree = GenerateTree();

            _scene.AddToScene(tree);

            tree.SetPosition(
                left: -1 * tree.Width * _scene.Scaling,
                top: (_scene.Height / 2 * _scene.Scaling));

            Console.WriteLine("Tree generated.");

            return true;
        }

        private Construct GenerateTree()
        {
            Tree tree = new(
                animateAction: AnimateTree,
                recycleAction: RecycleTree,
                scaling: _scene.Scaling);

            return tree;
        }

        private bool AnimateTree(Construct tree)
        {
            var speed = _scene.Speed + tree.SpeedOffset;
            MoveConstruct(tree, speed);
            return true;
        }

        private bool RecycleTree(Construct tree)
        {
            var hitBox = tree.GetHitBox();

            if (hitBox.Top > _scene.Height || hitBox.Left > _scene.Width)
                _scene.DisposeFromScene(tree);

            return true;
        }

        #endregion

        #region Honk

        public bool GenerateHonkInScene(Vehicle vehicle)
        {
            Honk honk = new(
                animateAction: AnimateHonk,
                recycleAction: RecycleHonk,
                scaling: _scene.Scaling)
            {
                SpeedOffset = vehicle.SpeedOffset * 1.3
            };

            var hitBox = vehicle.GetCloseHitBox();

            honk.SetPosition(
                left: hitBox.Left - vehicle.Width / 2,
                top: hitBox.Top - (25 * _scene.Scaling));

            honk.SetRotation(_random.Next(-30, 30));
            honk.SetZ(vehicle.GetZ() + 1);

            _scene.AddToScene(honk);

            return true;
        }

        public bool AnimateHonk(Construct honk)
        {
            var speed = _scene.Speed + honk.SpeedOffset;
            MoveConstruct(honk, speed);
            return true;
        }

        private bool RecycleHonk(Construct honk)
        {
            Honk honk1 = honk as Honk;

            honk1.Fade();

            if (honk1.IsFadingComplete)
                _scene.DisposeFromScene(honk);

            return true;
        }

        #endregion

        #region Construct

        private void MoveConstruct(Construct construct, double speed)
        {
            construct.SetLeft(construct.GetLeft() + speed);
            construct.SetTop(construct.GetTop() + speed * construct.Displacement);
        }

        #endregion

        #endregion

        #region Events

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            SizeChanged += MainPage_SizeChanged;
        }

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs args)
        {
            var _windowWidth = args.NewSize.Width;
            var _windowHeight = args.NewSize.Height;

            _scene.Width = _windowWidth;
            _scene.Height = _windowHeight;

            //_scene.Width = 1920;
            //_scene.Height = 1080;
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            SizeChanged -= MainPage_SizeChanged;
        }

        private void InputView_PointerMoved(object sender, PointerRoutedEventArgs e)
        {

        }

        private void InputView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Generator treeBottom = new(generationDelay: 40, generationAction: GenerateTreeInSceneBottom);
            Generator treeTop = new(generationDelay: 40, generationAction: GenerateTreeInSceneTop);

            Generator roadMark = new(generationDelay: 30, generationAction: GenerateRoadMarkInScene);
            Generator vehicle = new(generationDelay: 80, generationAction: GenerateVehicleInScene);

            _scene.AddToScene(treeBottom);
            _scene.AddToScene(treeTop);

            _scene.AddToScene(roadMark);
            _scene.AddToScene(vehicle);

            _scene.Speed = 5;
            _scene.Start();
        }

        #endregion
    }
}
