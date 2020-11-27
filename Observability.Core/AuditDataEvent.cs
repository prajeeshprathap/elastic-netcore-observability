using System;

namespace Observability.Core
{
    public abstract class AuditDataEvent
    {
        public AuditDataEvent()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; private set; }
    }
}
