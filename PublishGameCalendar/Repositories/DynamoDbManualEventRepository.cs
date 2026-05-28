using Amazon.DynamoDBv2.DataModel;
using AquaOs.Calendar.Data.DynamoDb;
using AquaOs.Calendar.Domain;

namespace AquaOs.Calendar.Repositories;

public class DynamoDbManualEventRepository : IManualEventRepository
{
    private readonly IDynamoDbContext _db;

    public DynamoDbManualEventRepository(IDynamoDbContext db)
    {
        _db = db;
    }

    public async Task<List<ManualEventEntity>> GetBySeriesIdAsync(string seriesId)
    {
        List<ScanCondition> conditions = new()
        {
            new ScanCondition("series_id", Amazon.DynamoDBv2.DocumentModel.ScanOperator.Equal, seriesId)
        };
        return await _db.ScanAsync<ManualEventEntity>(conditions);
    }

    public async Task<ManualEventEntity?> GetByIdAsync(string seriesId, string eventUid)
    {
        string pk = $"{seriesId}#{eventUid}";
        return await _db.LoadAsync<ManualEventEntity>(pk);
    }

    public Task<ManualEventEntity> CreateAsync(ManualEventEntity entity)
    {
        entity.Pk = $"{entity.SeriesId}#{entity.Uid}";
        entity.CreatedAt = DateTime.UtcNow.ToString("O");
        entity.UpdatedAt = entity.CreatedAt;
        return SaveAndReturn(entity);
    }

    public Task UpdateAsync(ManualEventEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow.ToString("O");
        return _db.SaveAsync(entity);
    }

    public async Task DeleteAsync(string seriesId, string eventUid)
    {
        string pk = $"{seriesId}#{eventUid}";
        await _db.DeleteAsync<ManualEventEntity>(pk);
    }

    private async Task<ManualEventEntity> SaveAndReturn(ManualEventEntity entity)
    {
        await _db.SaveAsync(entity);
        return entity;
    }
}