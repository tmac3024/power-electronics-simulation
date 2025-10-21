using System;
using System.Collections.Generic;
using System.Linq;
using PowerElectronicsSimulator.Models;

namespace PowerElectronicsSimulator.Services
{
    public class CircuitAnalyzer
    {
        private List<Component> _components;
        private List<Connection> _connections;

        public CircuitAnalyzer(List<Component> components, List<Connection> connections)
        {
            _components = components;
            _connections = connections;
        }

        // Analyze circuit and calculate voltages and currents
        public Dictionary<string, SimulationResult> AnalyzeCircuit(double inputVoltage, double frequency)
        {
            var results = new Dictionary<string, SimulationResult>();

            try
            {
                // Find voltage source
                var voltageSource = _components.OfType<VoltageSource>().FirstOrDefault();
                if (voltageSource == null)
                {
                    throw new InvalidOperationException("No voltage source found in circuit");
                }

                // Build circuit matrix (simplified nodal analysis)
                var nodes = BuildNodeList();
                var nodeVoltages = SolveNodalAnalysis(nodes, inputVoltage);

                // Calculate component-specific results
                foreach (var component in _components)
                {
                    var result = CalculateComponentResult(component, nodeVoltages, frequency);
                    results[component.Id] = result;
                }

                return results;
            }
            catch (Exception ex)
            {
                // Return error results
                foreach (var component in _components)
                {
                    results[component.Id] = new SimulationResult
                    {
                        ComponentId = component.Id,
                        Voltage = 0,
                        Current = 0,
                        Power = 0,
                        ErrorMessage = ex.Message
                    };
                }
                return results;
            }
        }

        // Build list of nodes in the circuit
        private List<CircuitNode> BuildNodeList()
        {
            var nodes = new Dictionary<string, CircuitNode>();
            int nodeCounter = 0;

            // Create nodes from connections
            foreach (var connection in _connections)
            {
                // From node
                if (!nodes.ContainsKey(connection.FromComponentId))
                {
                    nodes[connection.FromComponentId] = new CircuitNode
                    {
                        Id = $"Node{nodeCounter++}",
                        ComponentIds = new List<string> { connection.FromComponentId }
                    };
                }

                // To node
                if (!nodes.ContainsKey(connection.ToComponentId))
                {
                    nodes[connection.ToComponentId] = new CircuitNode
                    {
                        Id = $"Node{nodeCounter++}",
                        ComponentIds = new List<string> { connection.ToComponentId }
                    };
                }
            }

            return nodes.Values.ToList();
        }

        // Solve nodal analysis using simplified KCL/KVL
        private Dictionary<string, double> SolveNodalAnalysis(List<CircuitNode> nodes, double inputVoltage)
        {
            var voltages = new Dictionary<string, double>();

            // Ground reference (node 0 = 0V)
            voltages["Node0"] = 0;

            // Apply input voltage to first node
            if (nodes.Count > 0)
            {
                voltages[nodes[0].Id] = inputVoltage;
            }

            // Calculate voltages for remaining nodes (simplified approach)
            for (int i = 1; i < nodes.Count; i++)
            {
                var node = nodes[i];
                double voltage = CalculateNodeVoltage(node, voltages);
                voltages[node.Id] = voltage;
            }

            return voltages;
        }

        // Calculate voltage at a specific node
        private double CalculateNodeVoltage(CircuitNode node, Dictionary<string, double> knownVoltages)
        {
            // Simplified voltage calculation based on adjacent nodes
            double totalVoltage = 0;
            int count = 0;

            foreach (var componentId in node.ComponentIds)
            {
                var component = _components.FirstOrDefault(c => c.Id == componentId);
                if (component != null)
                {
                    // Get connected nodes
                    var connections = _connections.Where(c => 
                        c.FromComponentId == componentId || c.ToComponentId == componentId).ToList();

                    foreach (var conn in connections)
                    {
                        string adjacentNodeId = conn.FromComponentId == componentId ? 
                            $"Node{_components.FindIndex(c => c.Id == conn.ToComponentId)}" :
                            $"Node{_components.FindIndex(c => c.Id == conn.FromComponentId)}";

                        if (knownVoltages.ContainsKey(adjacentNodeId))
                        {
                            totalVoltage += knownVoltages[adjacentNodeId];
                            count++;
                        }
                    }
                }
            }

            return count > 0 ? totalVoltage / count : 0;
        }

        // Calculate results for a specific component
        private SimulationResult CalculateComponentResult(Component component, 
            Dictionary<string, double> nodeVoltages, double frequency)
        {
            var result = new SimulationResult
            {
                ComponentId = component.Id,
                ComponentName = component.Name
            };

            try
            {
                // Get node voltages for this component
                var nodeId = $"Node{_components.IndexOf(component)}";
                double voltage = nodeVoltages.ContainsKey(nodeId) ? nodeVoltages[nodeId] : 0;

                // Calculate based on component type
                if (component is Resistor resistor)
                {
                    result.Voltage = voltage;
                    result.Current = voltage / resistor.Value; // I = V/R
                    result.Power = voltage * result.Current; // P = V*I
                }
                else if (component is Capacitor capacitor)
                {
                    result.Voltage = voltage;
                    // Capacitive reactance: Xc = 1/(2*pi*f*C)
                    double capacitance = capacitor.Value * 1e-6; // Convert µF to F
                    double reactance = 1.0 / (2 * Math.PI * frequency * capacitance);
                    result.Current = voltage / reactance;
                    result.Power = 0; // Ideal capacitor doesn't dissipate power
                    result.Reactance = reactance;
                }
                else if (component is Inductor inductor)
                {
                    result.Voltage = voltage;
                    // Inductive reactance: XL = 2*pi*f*L
                    double inductance = inductor.Value * 1e-3; // Convert mH to H
                    double reactance = 2 * Math.PI * frequency * inductance;
                    result.Current = voltage / reactance;
                    result.Power = 0; // Ideal inductor doesn't dissipate power
                    result.Reactance = reactance;
                }
                else if (component is Diode diode)
                {
                    result.Voltage = voltage;
                    // Simplified diode model
                    if (voltage > diode.ForwardVoltage)
                    {
                        result.Current = (voltage - diode.ForwardVoltage) / 10; // Assume 10Ω forward resistance
                        result.Power = voltage * result.Current;
                    }
                    else
                    {
                        result.Current = 0;
                        result.Power = 0;
                    }
                }
                else if (component is Transistor transistor)
                {
                    result.Voltage = voltage;
                    // Simplified transistor model (assumed ON state)
                    double onResistance = transistor.OnResistance * 1e-3; // Convert mΩ to Ω
                    result.Current = voltage / onResistance;
                    result.Power = voltage * result.Current;
                }
                else if (component is Load load)
                {
                    result.Voltage = voltage;
                    result.Current = voltage / load.Value; // I = V/R
                    result.Power = voltage * result.Current; // P = V*I
                }
                else if (component is VoltageSource voltageSource)
                {
                    result.Voltage = voltageSource.Value;
                    result.Current = CalculateSourceCurrent(voltageSource);
                    result.Power = result.Voltage * result.Current;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Error calculating results: {ex.Message}";
                return result;
            }
        }

        // Calculate total current drawn from voltage source
        private double CalculateSourceCurrent(VoltageSource source)
        {
            double totalCurrent = 0;

            // Sum currents from all components connected to source
            var connectedComponents = _connections
                .Where(c => c.FromComponentId == source.Id || c.ToComponentId == source.Id)
                .Select(c => c.FromComponentId == source.Id ? c.ToComponentId : c.FromComponentId)
                .Distinct();

            foreach (var componentId in connectedComponents)
            {
                var component = _components.FirstOrDefault(c => c.Id == componentId);
                if (component is Resistor resistor)
                {
                    totalCurrent += source.Value / resistor.Value;
                }
                else if (component is Load load)
                {
                    totalCurrent += source.Value / load.Value;
                }
            }

            return totalCurrent;
        }

        // Calculate transfer function for the circuit
        public TransferFunction CalculateTransferFunction()
        {
            var tf = new TransferFunction();

            // Find key components
            var inductors = _components.OfType<Inductor>().ToList();
            var capacitors = _components.OfType<Capacitor>().ToList();
            var resistors = _components.OfType<Resistor>().ToList();

            if (inductors.Any() && capacitors.Any())
            {
                // LC circuit - second order
                var L = inductors.First().Value * 1e-3; // mH to H
                var C = capacitors.First().Value * 1e-6; // µF to F
                var R = resistors.Any() ? resistors.First().Value : 1.0;

                tf.NaturalFrequency = 1.0 / Math.Sqrt(L * C);
                tf.DampingRatio = (R / 2.0) * Math.Sqrt(C / L);
                tf.QFactor = 1.0 / (2.0 * tf.DampingRatio);
            }

            return tf;
        }
    }

    // Circuit node class
    public class CircuitNode
    {
        public string Id { get; set; }
        public List<string> ComponentIds { get; set; }
        public double Voltage { get; set; }
    }

    // Simulation result class
    public class SimulationResult
    {
        public string ComponentId { get; set; }
        public string ComponentName { get; set; }
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double Power { get; set; }
        public double Reactance { get; set; }
        public string ErrorMessage { get; set; }
    }

    // Transfer function class
    public class TransferFunction
    {
        public double NaturalFrequency { get; set; }
        public double DampingRatio { get; set; }
        public double QFactor { get; set; }
    }
}
