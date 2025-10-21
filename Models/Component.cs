using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PowerElectronicsSimulator.Models
{
    // Base class for all circuit components
    public abstract class Component : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private double _value;
        private string _unit;
        private double _x;
        private double _y;
        private double _width = 80;
        private double _height = 40;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public double Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(); }
        }

        public double X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(); }
        }

        public double Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(); }
        }

        public double Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(); }
        }

        public double Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(); }
        }

        public List<string> ConnectedTo { get; set; } = new List<string>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Abstract method to get component-specific properties for simulation
        public abstract Dictionary<string, object> GetSimulationParameters();
    }

    // Resistor component
    public class Resistor : Component
    {
        public Resistor()
        {
            Name = "Resistor";
            Unit = "Ω";
            Value = 1000; // Default 1kΩ
        }

        public override Dictionary<string, object> GetSimulationParameters()
        {
            return new Dictionary<string, object>
            {
                { "type", "resistor" },
                { "resistance", Value }
            };
        }
    }

    // Capacitor component
    public class Capacitor : Component
    {
        public Capacitor()
        {
            Name = "Capacitor";
            Unit = "µF";
            Value = 100; // Default 100µF
        }

        public override Dictionary<string, object> GetSimulationParameters()
        {
            return new Dictionary<string, object>
            {
                { "type", "capacitor" },
                { "capacitance", Value * 1e-6 } // Convert µF to F
            };
        }
    }

    // Inductor component
    public class Inductor : Component
    {
        public Inductor()
        {
            Name = "Inductor";
            Unit = "mH";
            Value = 10; // Default 10mH
        }

        public override Dictionary<string, object> GetSimulationParameters()
        {
            return new Dictionary<string, object>
            {
                { "type", "inductor" },
                { "inductance", Value * 1e-3 } // Convert mH to H
            };
        }
    }

    // Diode component
    public class Diode : Component
    {
        private double _forwardVoltage;

        public double ForwardVoltage
        {
            get => _forwardVoltage;
            set { _forwardVoltage = value; OnPropertyChanged(); }
        }

        public Diode()
        {
            Name = "Diode";
            Unit = "V";
            ForwardVoltage = 0.7; // Default forward voltage drop
        }

        public override Dictionary<string, object> GetSimulationParameters()
        {
            return new Dictionary<string, object>
            {
                { "type", "diode" },
                { "forward_voltage", ForwardVoltage }
            };
        }
    }

    // Transistor (MOSFET/IGBT) component
    public class Transistor : Component
    {
        private double _onResistance;
        private double _gateThreshold;

        public double OnResistance
        {
            get => _onResistance;
            set { _onResistance = value; OnPropertyChanged(); }
        }

        public double GateThreshold
        {
            get => _gateThreshold;
            set { _gateThreshold = value; OnPropertyChanged(); }
        }

        public Transistor()
        {
            Name = "Transistor";
            Unit = "mΩ";
            OnResistance = 10; // Default 10mΩ on-resistance
            GateThreshold = 2.5; // Default gate threshold voltage
        }

        public override Dictionary<string, object> GetSimulationParameters()
        {
            return new Dictionary<string, object>
            {
                { "type", "transistor" },
                { "on_resistance", OnResistance * 1e-3 },
                { "gate_threshold", GateThreshold }
            };
        }
    }

    // Voltage Source component
    public class VoltageSource : Component
    {
        public VoltageSource()
        {
            Name = "Voltage Source";
            Unit = "V";
            Value = 12; // Default 12V
        }

        public override Dictionary<string, object> GetSimulationParameters()
        {
            return new Dictionary<string, object>
            {
                { "type", "voltage_source" },
                { "voltage", Value }
            };
        }
    }

    // Load component
    public class Load : Component
    {
        private double _power;

        public double Power
        {
            get => _power;
            set { _power = value; OnPropertyChanged(); }
        }

        public Load()
        {
            Name = "Load";
            Unit = "Ω";
            Value = 50; // Default 50Ω load resistance
            Power = 0;
        }

        public override Dictionary<string, object> GetSimulationParameters()
        {
            return new Dictionary<string, object>
            {
                { "type", "load" },
                { "resistance", Value },
                { "power", Power }
            };
        }
    }

    // Connection/Wire class
    public class Connection : INotifyPropertyChanged
    {
        private string _id;
        private string _fromComponentId;
        private string _toComponentId;
        private double _x1, _y1, _x2, _y2;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string FromComponentId
        {
            get => _fromComponentId;
            set { _fromComponentId = value; OnPropertyChanged(); }
        }

        public string ToComponentId
        {
            get => _toComponentId;
            set { _toComponentId = value; OnPropertyChanged(); }
        }

        public double X1
        {
            get => _x1;
            set { _x1 = value; OnPropertyChanged(); }
        }

        public double Y1
        {
            get => _y1;
            set { _y1 = value; OnPropertyChanged(); }
        }

        public double X2
        {
            get => _x2;
            set { _x2 = value; OnPropertyChanged(); }
        }

        public double Y2
        {
            get => _y2;
            set { _y2 = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
