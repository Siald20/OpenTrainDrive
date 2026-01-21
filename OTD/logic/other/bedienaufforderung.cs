using System;
using System.Collections.Generic;

namespace OTD.logic.other
{
    /// <summary>
    /// Reprasentiert eine Bedienaufforderung fur ein Signal mit Status und Zeitstempeln.
    /// </summary>
    public class Bedienaufforderung
    {
        /// <summary>
        /// Erstellt eine neue Bedienaufforderung.
        /// </summary>
        /// <param name="signalId">Signalkennung.</param>
        /// <param name="message">Anzeigetext.</param>
        /// <param name="createdAtUtc">Erstellungszeit in UTC.</param>
        public Bedienaufforderung(string signalId, string message, DateTime createdAtUtc)
        {
            SignalId = signalId;
            Message = message;
            CreatedAtUtc = createdAtUtc;
            IsActive = true;
        }

        public string SignalId { get; }
        public string Message { get; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAtUtc { get; }
        public DateTime? AcknowledgedAtUtc { get; private set; }

        /// <summary>
        /// Quittiert die Bedienaufforderung und setzt den Zeitstempel.
        /// </summary>
        /// <param name="acknowledgedAtUtc">Quittierungszeit in UTC.</param>
        public void Acknowledge(DateTime acknowledgedAtUtc)
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            AcknowledgedAtUtc = acknowledgedAtUtc;
        }
    }

    /// <summary>
    /// Verwaltet aktive Bedienaufforderungen und meldet Ereignisse beim Erstellen und Quittieren.
    /// </summary>
    public class BedienaufforderungManager
    {
        private readonly Dictionary<string, Bedienaufforderung> _active = new(StringComparer.OrdinalIgnoreCase);

        public event Action<Bedienaufforderung>? Raised;
        public event Action<Bedienaufforderung>? Acknowledged;

        public IReadOnlyCollection<Bedienaufforderung> Active => _active.Values;

        /// <summary>
        /// Erstellt eine neue Bedienaufforderung fur ein Signal, falls noch keine aktiv ist.
        /// </summary>
        /// <param name="signalId">Signalkennung.</param>
        /// <param name="message">Optionaler Text.</param>
        /// <returns>Die vorhandene oder neu erstellte Aufforderung.</returns>
        public Bedienaufforderung Raise(string signalId, string message)
        {
            if (string.IsNullOrWhiteSpace(signalId))
            {
                return null;
            }

            var id = signalId.Trim();
            if (_active.TryGetValue(id, out var existing))
            {
                return existing;
            }

            var text = string.IsNullOrWhiteSpace(message) ? $"Bedienaufforderung {id}" : message.Trim();
            var item = new Bedienaufforderung(id, text, DateTime.UtcNow);
            _active[id] = item;
            Raised?.Invoke(item);
            return item;
        }

        /// <summary>
        /// Quittiert eine aktive Bedienaufforderung fur ein Signal.
        /// </summary>
        /// <param name="signalId">Signalkennung.</param>
        /// <returns>true, wenn eine aktive Aufforderung quittiert wurde.</returns>
        public bool Acknowledge(string signalId)
        {
            if (string.IsNullOrWhiteSpace(signalId))
            {
                return false;
            }

            var id = signalId.Trim();
            if (!_active.TryGetValue(id, out var item))
            {
                return false;
            }

            item.Acknowledge(DateTime.UtcNow);
            _active.Remove(id);
            Acknowledged?.Invoke(item);
            return true;
        }
    }
}
