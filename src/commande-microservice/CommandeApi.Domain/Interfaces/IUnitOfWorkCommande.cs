using System;
using System.Collections.Generic;
using System.Text;

namespace CommandeApi.Domain.Interfaces;

public interface IUnitOfWorkCommande : IDisposable
{
    ICommandeRepository CommandeRepository { get; }
}
