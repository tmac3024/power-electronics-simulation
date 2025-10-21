using System;
using System.Numerics;
using PowerElectronicsSimulator.Models;

namespace PowerElectronicsSimulator.Services
{
    public enum CompensatorType
    {
        Type1,  // Simple integrator
        Type2,  // PI controller
        Type3   // PID controller
    }

    public class CompensatorDesigner
    {
        public Circuit Circuit { get; set; }
        public CircuitAnalyzer Analyzer { get; set; }

        public CompensatorDesigner(Circuit circuit)
        {
            Circuit = circuit;
            Analyzer = new CircuitAnalyzer(circuit);
        }

        // Design Type 2 compensator (PI controller)
        public (double Kp, double Ki, double Fc, double Fz) DesignType2Compensator(double targetCrossover, double targetPhaseMargin)
        {
            // Calculate plant transfer function at crossover frequency
            var plantTF = Analyzer.CalculateTransferFunction(targetCrossover);
            double plantGain = Analyzer.CalculateGainDB(plantTF);
            double plantPhase = Analyzer.CalculatePhase(plantTF);

            // Calculate required compensator gain to achieve 0dB at crossover
            double compensatorGainDB = -plantGain;
            double compensatorGainLinear = Math.Pow(10, compensatorGainDB / 20);

            // Calculate required phase boost
            double requiredPhase = targetPhaseMargin - (180 + plantPhase) + 5; // 5 degree safety margin

            // For Type 2: One zero to provide phase boost
            // Place zero at fc/10 (typical)
            double fz = targetCrossover / 10;
            double fc = targetCrossover; // Crossover frequency

            // Calculate Ki and Kp
            double Ki = compensatorGainLinear * 2 * Math.PI * fz;
            double Kp = compensatorGainLinear;

            return (Kp, Ki, fc, fz);
        }

        // Design Type 3 compensator (PID controller)
        public (double Kp, double Ki, double Kd, double Fc, double Fz1, double Fz2, double Fp1, double Fp2) DesignType3Compensator(
            double targetCrossover, double targetPhaseMargin)
        {
            // Calculate plant transfer function at crossover frequency
            var plantTF = Analyzer.CalculateTransferFunction(targetCrossover);
            double plantGain = Analyzer.CalculateGainDB(plantTF);
            double plantPhase = Analyzer.CalculatePhase(plantTF);

            // Calculate required compensator gain
            double compensatorGainDB = -plantGain;
            double compensatorGainLinear = Math.Pow(10, compensatorGainDB / 20);

            // Calculate required phase boost
            double requiredPhase = targetPhaseMargin - (180 + plantPhase) + 10; // 10 degree safety margin

            // Type 3: Two zeros and two poles
            // Place zeros to provide phase boost
            double fz1 = targetCrossover / 10;
            double fz2 = targetCrossover / 5;

            // Place poles to reduce high-frequency gain
            double fp1 = targetCrossover * 5;
            double fp2 = targetCrossover * 10;

            // Calculate PID parameters
            double Kp = compensatorGainLinear;
            double Ki = compensatorGainLinear * 2 * Math.PI * fz1;
            double Kd = compensatorGainLinear / (2 * Math.PI * fp1);

            return (Kp, Ki, Kd, targetCrossover, fz1, fz2, fp1, fp2);
        }

        // Calculate compensator transfer function
        public Complex CalculateCompensatorTF(double frequency, CompensatorType type, params double[] parameters)
        {
            double omega = 2 * Math.PI * frequency;
            Complex s = new Complex(0, omega);

            switch (type)
            {
                case CompensatorType.Type1:
                    // H(s) = Ki/s
                    double Ki1 = parameters[0];
                    return Ki1 / s;

                case CompensatorType.Type2:
                    // H(s) = Kp + Ki/s = Kp(1 + s/wz) / s
                    double Kp2 = parameters[0];
                    double Ki2 = parameters[1];
                    return Kp2 + Ki2 / s;

                case CompensatorType.Type3:
                    // H(s) = Kp + Ki/s + Kd*s
                    double Kp3 = parameters[0];
                    double Ki3 = parameters[1];
                    double Kd3 = parameters[2];
                    return Kp3 + Ki3 / s + Kd3 * s;

                default:
                    return Complex.Zero;
            }
        }

        // Calculate open-loop transfer function (Plant * Compensator)
        public Complex CalculateOpenLoopTF(double frequency, CompensatorType type, params double[] parameters)
        {
            var plantTF = Analyzer.CalculateTransferFunction(frequency);
            var compensatorTF = CalculateCompensatorTF(frequency, type, parameters);
            return plantTF * compensatorTF;
        }

        // Calculate closed-loop transfer function
        public Complex CalculateClosedLoopTF(double frequency, CompensatorType type, params double[] parameters)
        {
            var openLoopTF = CalculateOpenLoopTF(frequency, type, parameters);
            return openLoopTF / (1 + openLoopTF);
        }

        // Verify compensator design by calculating margins
        public (double CrossoverFreq, double PhaseMargin, double GainMargin) VerifyDesign(
            CompensatorType type, params double[] parameters)
        {
            // Find crossover frequency for open-loop system
            double crossoverFreq = FindCrossoverFrequency(type, parameters, 1, 1000000);

            // Calculate phase margin
            var openLoopTF = CalculateOpenLoopTF(crossoverFreq, type, parameters);
            double phase = Math.Atan2(openLoopTF.Imaginary, openLoopTF.Real) * 180 / Math.PI;
            double phaseMargin = 180 + phase;

            // Calculate gain margin (find frequency where phase = -180)
            double freq180 = FindPhase180Frequency(type, parameters, 1, 1000000);
            var tf180 = CalculateOpenLoopTF(freq180, type, parameters);
            double gainMargin = -20 * Math.Log10(tf180.Magnitude);

            return (crossoverFreq, phaseMargin, gainMargin);
        }

        private double FindCrossoverFrequency(CompensatorType type, double[] parameters, double startFreq, double endFreq)
        {
            int iterations = 100;
            double tolerance = 0.01;

            for (int i = 0; i < iterations; i++)
            {
                double midFreq = (startFreq + endFreq) / 2;
                var tf = CalculateOpenLoopTF(midFreq, type, parameters);
                double gainDB = 20 * Math.Log10(tf.Magnitude);

                if (Math.Abs(gainDB) < tolerance)
                    return midFreq;

                if (gainDB > 0)
                    startFreq = midFreq;
                else
                    endFreq = midFreq;
            }

            return (startFreq + endFreq) / 2;
        }

        private double FindPhase180Frequency(CompensatorType type, double[] parameters, double startFreq, double endFreq)
        {
            int iterations = 100;
            double tolerance = 1.0;

            for (int i = 0; i < iterations; i++)
            {
                double midFreq = (startFreq + endFreq) / 2;
                var tf = CalculateOpenLoopTF(midFreq, type, parameters);
                double phase = Math.Atan2(tf.Imaginary, tf.Real) * 180 / Math.PI;

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
