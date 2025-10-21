using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PowerElectronicsSimulator.Models;

namespace PowerElectronicsSimulator.Services
{
    public class CircuitAnalyzer
    {
        public Circuit Circuit { get; set; }

        public CircuitAnalyzer(Circuit circuit)
        {
            Circuit = circuit;
        }

        // Calculate impedance at a given frequency
        public Complex CalculateImpedance(Component component, double frequency)
        {
            double omega = 2 * Math.PI * frequency;

            switch (component.Type)
            {
                case ComponentType.Resistor:
                    return new Complex(component.Value, 0);
                
                case ComponentType.Capacitor:
                    return new Complex(0, -1.0 / (omega * component.Value));
                
                case ComponentType.Inductor:
                    return new Complex(0, omega * component.Value);
                
                default:
                    return Complex.Zero;
            }
        }

        // Calculate transfer function H(s) = Vout/Vin
        public Complex CalculateTransferFunction(double frequency)
        {
            // Simplified transfer function for buck converter
            // This is a basic implementation - can be extended for more complex circuits
            
            var resistor = Circuit.Components.FirstOrDefault(c => c.Type == ComponentType.Resistor);
            var capacitor = Circuit.Components.FirstOrDefault(c => c.Type == ComponentType.Capacitor);
            var inductor = Circuit.Components.FirstOrDefault(c => c.Type == ComponentType.Inductor);

            if (resistor == null || capacitor == null || inductor == null)
                return Complex.Zero;

            double R = resistor.Value;
            double L = inductor.Value;
            double C = capacitor.Value;
            double omega = 2 * Math.PI * frequency;
            Complex s = new Complex(0, omega);

            // Transfer function: H(s) = R / (L*C*s^2 + R*C*s + 1)
            Complex numerator = R;
            Complex denominator = L * C * s * s + R * C * s + 1;

            return numerator / denominator;
        }

        // Calculate gain in dB
        public double CalculateGainDB(Complex transferFunction)
        {
            return 20 * Math.Log10(transferFunction.Magnitude);
        }

        // Calculate phase in degrees
        public double CalculatePhase(Complex transferFunction)
        {
            return transferFunction.Phase * 180 / Math.PI;
        }

        // Frequency response analysis
        public List<(double Frequency, double GainDB, double Phase)> AnalyzeFrequencyResponse(double startFreq, double endFreq, int points)
        {
            var results = new List<(double, double, double)>();
            double logStart = Math.Log10(startFreq);
            double logEnd = Math.Log10(endFreq);
            double step = (logEnd - logStart) / (points - 1);

            for (int i = 0; i < points; i++)
            {
                double frequency = Math.Pow(10, logStart + i * step);
                var tf = CalculateTransferFunction(frequency);
                double gain = CalculateGainDB(tf);
                double phase = CalculatePhase(tf);
                results.Add((frequency, gain, phase));
            }

            return results;
        }

        // Find crossover frequency (gain = 0 dB)
        public double FindCrossoverFrequency(double startFreq, double endFreq)
        {
            int iterations = 100;
            double tolerance = 0.01;

            for (int i = 0; i < iterations; i++)
            {
                double midFreq = (startFreq + endFreq) / 2;
                var tf = CalculateTransferFunction(midFreq);
                double gain = CalculateGainDB(tf);

                if (Math.Abs(gain) < tolerance)
                    return midFreq;

                if (gain > 0)
                    startFreq = midFreq;
                else
                    endFreq = midFreq;
            }

            return (startFreq + endFreq) / 2;
        }

        // Calculate phase margin at crossover frequency
        public double CalculatePhaseMargin(double crossoverFreq)
        {
            var tf = CalculateTransferFunction(crossoverFreq);
            double phase = CalculatePhase(tf);
            return 180 + phase; // Phase margin = 180° + phase at crossover
        }

        // Calculate gain margin
        public double CalculateGainMargin()
        {
            // Find frequency where phase = -180°
            double freq = FindPhase180Frequency(1, 1000000);
            var tf = CalculateTransferFunction(freq);
            return -CalculateGainDB(tf); // Gain margin = -gain at -180° phase
        }

        private double FindPhase180Frequency(double startFreq, double endFreq)
        {
            int iterations = 100;
            double tolerance = 1.0;

            for (int i = 0; i < iterations; i++)
            {
                double midFreq = (startFreq + endFreq) / 2;
                var tf = CalculateTransferFunction(midFreq);
                double phase = CalculatePhase(tf);

                if (Math.Abs(phase + 180) < tolerance)
                    return midFreq;

                if (phase > -180)
                    startFreq = midFreq;
                else
                    endFreq = midFreq;
            }

            return (startFreq + endFreq) / 2;
        }
    }
}
