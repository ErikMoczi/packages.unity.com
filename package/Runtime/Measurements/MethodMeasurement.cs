﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.PerformanceTesting.Exceptions;
using Unity.PerformanceTesting.Runtime;
using UnityEngine;


namespace Unity.PerformanceTesting.Measurements
{
    public class MethodMeasurement
    {
        private const int k_MeasurementCount = 7;
        private const int k_MinMeasurementTimeMs = 1;
        private const int k_MinWarmupTimeMs = 7;
        private const int k_ProbingMultiplier = 4;
        private const int k_MaxIterations = 10000;
        private readonly Action m_Action;
        private readonly List<SampleGroup> m_SampleGroups = new List<SampleGroup>();

        private SampleGroupDefinition m_Definition;
        private int m_WarmupCount;
        private int m_MeasurementCount;
        private int m_IterationCount = 1;

        public MethodMeasurement(Action action)
        {
            m_Action = action;
        }

        public MethodMeasurement ProfilerMarkers(SampleGroupDefinition[] profilerDefinitions)
        {
            if (profilerDefinitions == null) return this;
            AddProfilerMarkers(profilerDefinitions);
            return this;
        }

        private void AddProfilerMarkers(SampleGroupDefinition[] samplesGroup)
        {
            foreach (var sample in samplesGroup)
            {
                var sampleGroup = new SampleGroup(sample);
                sampleGroup.GetRecorder();
                sampleGroup.Recorder.enabled = false;
                m_SampleGroups.Add(sampleGroup);
            }
        }

        public MethodMeasurement Definition(SampleGroupDefinition definition)
        {
            m_Definition = definition;
            return this;
        }

        public MethodMeasurement Definition(string name = "Totaltime", SampleUnit sampleUnit = SampleUnit.Millisecond,
            AggregationType aggregationType = AggregationType.Median, double threshold = 0.1D,
            bool increaseIsBetter = false, bool failOnBaseline = true)
        {
            return Definition(new SampleGroupDefinition(name, sampleUnit, aggregationType, threshold, increaseIsBetter,
                failOnBaseline));
        }

        public MethodMeasurement Definition(string name, SampleUnit sampleUnit, AggregationType aggregationType,
            double percentile, double threshold = 0.1D, bool increaseIsBetter = false, bool failOnBaseline = true)
        {
            return Definition(new SampleGroupDefinition(name, sampleUnit, aggregationType, percentile, threshold,
                increaseIsBetter, failOnBaseline));
        }

        public MethodMeasurement WarmupCount(int count)
        {
            m_WarmupCount = count;
            return this;
        }

        public MethodMeasurement IterationsPerMeasurement(int count)
        {
            m_IterationCount = count;
            return this;
        }

        public MethodMeasurement MeasurementCount(int count)
        {
            m_MeasurementCount = count;
            return this;
        }
        
        public void Run()
        {
            if (m_MeasurementCount > 0)
            {
                Warmup(m_WarmupCount);
                RunForIterations(m_IterationCount, m_MeasurementCount);
                return;
            }
            
            var iterations = Probing();
            RunForIterations(iterations, k_MeasurementCount);
        }

        private void RunForIterations(int iterations, int measurements)
        {
            UpdateSampleGroupDefinition();

            EnableMarkers();
            for (int j = 0; j < measurements; j++)
            {
                var executionTime = Time.realtimeSinceStartup;
                for (var i = 0; i < iterations; i++)
                {
                    m_Action.Invoke();
                }
                executionTime = (Time.realtimeSinceStartup - executionTime) * 1000f / iterations;
                Measure.Custom(m_Definition, Utils.ConvertSample(SampleUnit.Millisecond, m_Definition.SampleUnit, executionTime));
            }

            DisableAndMeasureMarkers();
        }

        private void EnableMarkers()
        {
            foreach (var sampleGroup in m_SampleGroups)
            {
                sampleGroup.Recorder.enabled = true;
            }
        }

        private void DisableAndMeasureMarkers()
        {
            foreach (var sampleGroup in m_SampleGroups)
            {
                sampleGroup.Recorder.enabled = false;
                var sample = sampleGroup.Recorder.elapsedNanoseconds;
                var blockCount = sampleGroup.Recorder.sampleBlockCount;
                Measure.Custom(sampleGroup.Definition,
                    Utils.ConvertSample(SampleUnit.Nanosecond, sampleGroup.Definition.SampleUnit,
                        sample / blockCount));
            }
        }

        private int Probing()
        {
            var executionTime = 0.0f;
            var iterations = 1;

            while (executionTime < k_MinWarmupTimeMs)
            {
                executionTime = Time.realtimeSinceStartup;
                Warmup(iterations);
                executionTime = (Time.realtimeSinceStartup - executionTime) * 1000f;

                if (executionTime < k_MinWarmupTimeMs)
                {
                    iterations *= k_ProbingMultiplier;
                }
            }

            if (iterations == 1)
            {
                m_Action.Invoke();
                m_Action.Invoke();

                return 1;
            }

            var deisredIterationsCount =
                Mathf.Clamp((int) (k_MinMeasurementTimeMs * iterations / executionTime), 1, k_MaxIterations);

            return deisredIterationsCount;
        }

        private void Warmup(int iterations)
        {
            for (var i = 0; i < iterations; i++)
            {
                m_Action.Invoke();
            }
        }

        private void UpdateSampleGroupDefinition()
        {
            if (m_Definition.Name == null)
            {
                m_Definition = new SampleGroupDefinition("Time");
            }
        }
    }
}