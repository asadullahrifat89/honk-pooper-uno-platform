﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace HonkTrooper
{
    public partial class AudioStub
    {
        private readonly Random _random;
        private readonly List<(Audio[] AudioSources, Audio AudioInstance, SoundType SoundType)> _audioTuples = new();

        public AudioStub(params (SoundType SoundType, double Volume, bool Loop)[] soundInputs)
        {
            _random = new Random();

            foreach (var soundInput in soundInputs)
            {
                var audioSources = Constants.SOUND_TEMPLATES.Where(x => x.SoundType == soundInput.SoundType).Select(x => x.Uri).Select(uri => new Audio(uri: uri, volume: soundInput.Volume, loop: soundInput.Loop)).ToArray();
                var audioInstance = audioSources[_random.Next(audioSources.Length)];
                var soundType = soundInput.SoundType;

                _audioTuples.Add((audioSources, audioInstance, soundType));
            }
        }

        public void Play(params SoundType[] soundTypes)
        {
            foreach (var soundType in soundTypes)
            {
                var audioTuple = _audioTuples.FirstOrDefault(x => x.SoundType == soundType);
                audioTuple.AudioInstance?.Stop();
                audioTuple.AudioInstance = audioTuple.AudioSources[_random.Next(audioTuple.AudioSources.Length)];
                audioTuple.AudioInstance.Play();
            }           
        }

        public void Pause(params SoundType[] soundTypes)
        {
            foreach (var soundType in soundTypes)
            {
                var audioTuple = _audioTuples.FirstOrDefault(x => x.SoundType == soundType);
                audioTuple.AudioInstance?.Pause();
            }            
        }

        public void Resume(params SoundType[] soundTypes)
        {
            foreach (var soundType in soundTypes)
            {
                var audioTuple = _audioTuples.FirstOrDefault(x => x.SoundType == soundType);
                audioTuple.AudioInstance?.Resume();
            }
        }

        public void Stop(params SoundType[] soundTypes)
        {
            foreach (var soundType in soundTypes)
            {
                var audioTuple = _audioTuples.FirstOrDefault(x => x.SoundType == soundType);
                audioTuple.AudioInstance?.Stop();
            }
        }

        public void SetVolume(SoundType soundType, double volume)
        {
            var audioTuple = _audioTuples.FirstOrDefault(x => x.SoundType == soundType);
            audioTuple.AudioInstance?.SetVolume(volume);
        }
    }
}