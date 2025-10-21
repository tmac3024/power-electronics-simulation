using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerElectronicsSimulator.Models
{
    public enum ComponentType
    {
        Resistor,
        Capacitor,
        Inductor,
        VoltageSource,
        CurrentSource,
        Diode,
        MOSFET,
        BJT,
        OpAmp
    }

    public class Component
    {
        public string Name { get; set; }
        public ComponentType Type { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public int Node1 { get; set; }
        public int Node2 { get; set; }
        public int Node3 { get; set; } // For 3-terminal devices

        public Component(string name, ComponentType type, double value, string unit)
        {
            Name = name;
            Type = type;
            Value = value;
            Unit = unit;
        }

        public override string ToString()
        {
            return $"{Name}: {Value}{Unit}";
        }
    }

    public class Circuit
    {
        public List<Component> Components { get; set; } = new List<Component>();
        public List<Connection> Connections { get; set; } = new List<Connection>();
        public double Frequency { get; set; } = 100000; // Default 100kHz

        public void AddComponent(Component component)
        {
            Components.Add(component);
        }

        public void AddConnection(int node1, int node2, Component component)
        {
            Connections.Add(new Connection { Node1 = node1, Node2 = node2, Component = component });
        }
    }

    public class Connection
    {
        public int Node1 { get; set; }
        public int Node2 { get; set; }
        public Component Component { get; set; }
    }

    public class OperatingPoint
    {
        public Dictionary<int, double> NodeVoltages { get; set; } = new Dictionary<int, double>();
        public Dictionary<string, double> ComponentCurrents { get; set; } = new Dictionary<string, double>();
    }

    public class Wire
    {
        public int StartNode { get; set; }
        public int EndNode { get; set; }
        public System.Windows.Point StartPoint { get; set; }
        public System.Windows.Point EndPoint { get; set; }
    }
}
