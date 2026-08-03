using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Domain.Common
{
    public interface IEntity
    {
        Guid Id { get; set; }
    }
}
