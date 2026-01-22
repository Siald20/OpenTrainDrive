using System;
using System.Xml.Linq;
using System.Linq;
using System.Threading.Tasks;

namespace OpenTrainDrive.Engines
{
    /// <summary>
    /// Configuration values for acceleration behavior.
    /// Range: 1-255, where 1 is smoothest and 255 is most aggressive.
    /// Typical values: 1-50 (smooth), 50-150 (gradual), 150-255 (aggressive)
    /// </summary>
    public static class AccelerationConfig
    {
        public const int Smooth = 25;      // Smooth acceleration
        public const int Gradual = 75;     // Linear acceleration
        public const int Aggressive = 150; // Rapid acceleration
    }

    /// <summary>
    /// Configuration values for braking behavior.
    /// Range: 1-255, where 1 is smoothest and 255 is most aggressive.
    /// Typical values: 1-50 (smooth), 50-150 (gradual), 150-255 (emergency)
    /// </summary>
    public static class BrakingConfig
    {
        public const int Smooth = 25;      // Smooth braking
        public const int Gradual = 75;     // Linear deceleration
        public const int Emergency = 150;  // Rapid braking
    }

    /// <summary>
    /// Represents a model railway locomotive and provides control methods
    /// </summary>
    public class LocomotiveController
    {
        private string _locoXmlPath;
        private int _address;
        private XElement _locoElement;
        
        // Locomotive properties
        public string Name { get; private set; }
        public string UID { get; private set; }
        public int Length { get; private set; }
        public int Axles { get; private set; }
        public int Index { get; private set; }
        public int MaxVelocity { get; private set; }
        public string Manufacturer { get; private set; }
        public string Scale { get; private set; }
        public string CatalogNumber { get; private set; }
        public int DccAddress { get; private set; }

        /// <summary>
        /// Initializes a new instance of LocomotiveController with a specific locomotive address
        /// </summary>
        /// <param name="locoXmlPath">Path to the loco.xml file</param>
        /// <param name="address">DCC address of the locomotive to control</param>
        /// <exception cref="ArgumentException">Thrown when locomotive with specified address is not found</exception>
        public LocomotiveController(string locoXmlPath, int address)
        {
            if (!System.IO.File.Exists(locoXmlPath))
                throw new FileNotFoundException($"Locomotive configuration file not found: {locoXmlPath}");

            _locoXmlPath = locoXmlPath;
            _address = address;

            LoadLocomotiveConfiguration();
        }

        /// <summary>
        /// Loads locomotive configuration from loco.xml based on the DCC address
        /// </summary>
        private void LoadLocomotiveConfiguration()
        {
            try
            {
                XDocument doc = XDocument.Load(_locoXmlPath);
                
                _locoElement = doc.Descendants("loco")
                    .FirstOrDefault(l => 
                    {
                        var decoderAddress = l.Descendants("decoder")
                            .FirstOrDefault()?
                            .Element("address");
                        return decoderAddress != null && int.Parse(decoderAddress.Value) == _address;
                    });

                if (_locoElement == null)
                    throw new ArgumentException($"No locomotive found with DCC address {_address}");

                // Extract locomotive properties
                UID = _locoElement.Attribute("uid")?.Value ?? "unknown";
                Name = _locoElement.Attribute("name")?.Value ?? "Unknown Locomotive";
                Length = int.TryParse(_locoElement.Attribute("length")?.Value, out int len) ? len : 0;
                Axles = int.TryParse(_locoElement.Attribute("axles")?.Value, out int axles) ? axles : 0;
                Index = int.TryParse(_locoElement.Attribute("index")?.Value, out int idx) ? idx : 0;

                // Extract model properties
                var modelElement = _locoElement.Element("model");
                if (modelElement != null)
                {
                    Manufacturer = modelElement.Attribute("manufacturer")?.Value ?? "Unknown";
                    Scale = modelElement.Attribute("scale")?.Value ?? "Unknown";
                    CatalogNumber = modelElement.Attribute("catalognumber")?.Value ?? "Unknown";
                    
                    var vmaxElement = modelElement.Element("vmax");
                    MaxVelocity = vmaxElement != null && int.TryParse(vmaxElement.Value, out int vmax) ? vmax : 200;
                }

                // Extract decoder address
                var decoderElement = _locoElement.Element("decoder");
                if (decoderElement != null)
                {
                    var addressElement = decoderElement.Element("address");
                    DccAddress = addressElement != null && int.TryParse(addressElement.Value, out int addr) ? addr : _address;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error loading locomotive configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executes a run command for the locomotive
        /// </summary>
        /// <param name="address">DCC address of the locomotive</param>
        /// <param name="distance">Distance to travel in millimeters</param>
        /// <param name="vmax">Maximum velocity during acceleration phase (0-255)</param>
        /// <param name="vtarget">Target velocity after reaching distance (0-255)</param>
        /// <param name="accBehavior">Acceleration behavior (1-255). 1=smoothest, 255=most aggressive</param>
        /// <param name="brkBehavior">Braking behavior (1-255). 1=smoothest, 255=most aggressive</param>
        /// <returns>A Task representing the asynchronous operation</returns>
        public async Task Run(int address, int distance, int vmax, int vtarget, 
            int accBehavior, int brkBehavior)
        {
            if (address != _address)
                throw new InvalidOperationException($"Address mismatch. Controller is for address {_address}, but {address} was provided.");

            if (distance <= 0)
                throw new ArgumentException("Distance must be greater than 0", nameof(distance));


            if (vmax < 0 || vmax > 500)
                throw new ArgumentException("Vmax must be between 0 and 500", nameof(vmax));

            if (vtarget < 0 || vtarget > 500)
                throw new ArgumentException("Vtarget must be between 0 and 500", nameof(vtarget));

            if (accBehavior < 1 || accBehavior > 255)
                throw new ArgumentException("Acceleration behavior must be between 1 and 255", nameof(accBehavior));

            if (brkBehavior < 1 || brkBehavior > 255)
                throw new ArgumentException("Braking behavior must be between 1 and 255", nameof(brkBehavior));

            try
            {
                // Phase 1: Acceleration
                await AccelerateAsync(vmax, accBehavior);

                // Phase 2: Travel at maximum velocity
                await TravelAsync(distance, vmax);

                // Phase 3: Deceleration to target velocity
                if (vtarget < vmax)
                {
                    await DecelerateAsync(vtarget, brkBehavior);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error executing run command: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Accelerates the locomotive to the specified velocity
        /// </summary>
        /// <param name="targetVelocity">Target velocity (0-255)</param>
        /// <param name="behavior">Acceleration behavior value (1-255). Lower values = smoother, higher = more aggressive</param>
        private async Task AccelerateAsync(int targetVelocity, int behavior)
        {
            int currentVelocity = 0;
            // Step size increases with behavior value: behavior / 10 (range: 0.1-25.5, rounded to 1-25)
            int stepSize = Math.Max(1, behavior / 10);
            
            // Step delay decreases with behavior value: 200 - (behavior * 0.7)
            // Range: ~30ms (aggressive) to ~195ms (smooth)
            int stepDelay = Math.Max(10, 200 - (behavior * 7 / 10));

            while (currentVelocity < targetVelocity)
            {
                currentVelocity = Math.Min(currentVelocity + stepSize, targetVelocity);
                // Here you would send the velocity command to the DCC decoder
                // SendVelocityCommand(_address, currentVelocity);
                await Task.Delay(stepDelay);
            }
        }

        /// <summary>
        /// Travels the specified distance at the given velocity
        /// </summary>
        private async Task TravelAsync(int distance, int velocity)
        {
            // Assuming average speed of velocity * 0.1 mm/ms (simplified model)
            // This would need calibration based on actual locomotive performance
            int estimatedTime = (int)(distance / (velocity * 0.1)); // milliseconds
            
            // Ensure minimum travel time of 100ms
            estimatedTime = Math.Max(estimatedTime, 100);
            
            // Here you would maintain the velocity during travel
            // SendVelocityCommand(_address, velocity);
            await Task.Delay(estimatedTime);
        }

        /// <summary>
        /// Decelerates the locomotive to the specified velocity
        /// </summary>
        /// <param name="targetVelocity">Target velocity (0-255)</param>
        /// <param name="behavior">Braking behavior value (1-255). Lower values = smoother, higher = more aggressive</param>
        private async Task DecelerateAsync(int targetVelocity, int behavior)
        {
            // This would be implemented by reading current velocity and gradually decreasing it
            // For now, we'll assume starting from maximum velocity
            int currentVelocity = MaxVelocity;
            // Step size increases with behavior value: behavior / 10 (range: 0.1-25.5, rounded to 1-25)
            int stepSize = Math.Max(1, behavior / 10);
            
            // Step delay decreases with behavior value: 200 - (behavior * 0.7)
            // Range: ~30ms (aggressive) to ~195ms (smooth)
            int stepDelay = Math.Max(5, 200 - (behavior * 7 / 10));

            while (currentVelocity > targetVelocity)
            {
                currentVelocity = Math.Max(currentVelocity - stepSize, targetVelocity);
                // Here you would send the velocity command to the DCC decoder
                // SendVelocityCommand(_address, currentVelocity);
                await Task.Delay(stepDelay);
            }
        }

        /// <summary>
        /// Gets the current status of the locomotive
        /// </summary>
        public override string ToString()
        {
            return $"Locomotive: {Name} (UID: {UID}, DCC Address: {DccAddress}, " +
                   $"Manufacturer: {Manufacturer}, Scale: {Scale}, MaxVelocity: {MaxVelocity} km/h)";
        }
    }
}
