using Ardalis.Result;
using Core;
using Core.Interfaces;
using Mapster;
using MySqlX.XDevAPI.Common;
using ProductApi.Domain.Interfaces;
using ProductApi.Domain.Models;
using ProductApi.Infrastructure.Entities;

namespace ProductApi.Infrastructure.Repositories;

internal class CategorieRepository : ICategorieRepository
{
    private readonly IRepositoryBase<Categorie> _persistence;
    public CategorieRepository(IRepositoryBase<Categorie> persistence)
    {
        _persistence = persistence;
    }

    public async Task<CategoriePOCO> AddCategorieAsync(CategoriePOCO categorie)
    {
        var entity = categorie.Adapt<Categorie>();
        var resultat = await _persistence.AddAsync(entity);
        return resultat.Adapt<CategoriePOCO>();
    }

    public async Task<IEnumerable<CategoriePOCO>> GetAllCategorietAsync()
    {
        var resultat = _persistence.GetAll().ToList();
        return resultat.Adapt<IEnumerable<CategoriePOCO>>();
    }

    public async Task<CategoriePOCO?> GetCategorieByIdAsync(int Id)
    {
        var resultat = await _persistence.GetByIdAsync(Id);
        return resultat!.Adapt<CategoriePOCO>();
    }

    public async Task<bool> RemoveCategorieAsync(int Id)
    {
        var res = await _persistence.DeleteAsync(Id);

        if (!res)
        {
            throw new Exception($"La catégorie est introuvable {Id}");
        }

        return res;
    }
}
