﻿using Al;

using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using SZ.Core.Abstractions.Interfaces;
using SZ.Core.Models;
using SZ.Core.Models.Interfaces;

namespace SZ.Core
{
    public class CRUDManager<T, TCreate, TUpdate, TDelete> : ICRUDManager<T, TCreate, TUpdate, TDelete>
        where T : class, IDBEntity
    {
        readonly ILogger _logger;
        public CRUDManager(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger(GetType());
        }
        public Func<Result<T>, DBProvider, TCreate, CancellationToken, Task> ValidCreateModel { get; init; }
        public Func<Result<T>, DBProvider, TUpdate, CancellationToken, Task> ValidUpdateModel { get; init; }
        public Func<Result<T>, DBProvider, TDelete, CancellationToken, Task> ValidDeleteModel { get; init; }


        /// <summary>
        /// Валидация права создания сущности. Коды ошибок от 100-199
        /// </summary>
        public Func<Result<T>, DBProvider, TCreate, IUserSessionService, CancellationToken, Task> ValidCreateRight { get; init; }
        /// <summary>
        /// Валидация права изменения сущности. Коды ошибок от 100-199
        /// </summary>
        public Func<Result<T>, DBProvider, TUpdate, IUserSessionService, CancellationToken, Task> ValidUpdateRight { get; init; }
        /// <summary>
        /// Валидация права удаления сущности. Коды ошибок от 100-199
        /// </summary>
        public Func<Result<T>, DBProvider, TDelete, IUserSessionService, CancellationToken, Task> ValidDeleteRight { get; init; }
        /// <summary>
        /// Операция подготовки создания сущности. Коды ошибок 400-499
        /// </summary>
        public Func<Result<T>, DBProvider, TCreate, IUserSessionService, CancellationToken, Task<T>> PrepareCreate { get; init; }
        /// <summary>
        /// Операция подготовки создания сущности. Коды ошибок 400-499
        /// </summary>
        public Func<Result<T>, DBProvider, TCreate, IUserSessionService, CancellationToken, Task> PostCreate { get; init; }
        /// <summary>
        /// Операция подготовки изменения сущности. Коды ошибок 400-499
        /// </summary>
        public Func<Result<T>, DBProvider, TUpdate, IUserSessionService, CancellationToken, Task<T>> PrepareUpdate { get; init; }
        /// <summary>
        /// Операция подготовки изменения сущности. Коды ошибок 400-499
        /// </summary>
        public Func<Result<T>, DBProvider, TUpdate, IUserSessionService, CancellationToken, Task> PostUpdate { get; init; }
        /// <summary>
        /// Операция подготовки удаления сущности. Коды ошибок 400-499
        /// </summary>
        public Func<Result<T>, DBProvider, TDelete, IUserSessionService, CancellationToken, Task<T>> PrepareDelete { get; init; }
        /// <summary>
        /// Операция подготовки удаления сущности. Коды ошибок 400-499
        /// </summary>
        public Func<Result<T>, DBProvider, TDelete, IUserSessionService, CancellationToken, Task> PostDelete { get; init; }



        public async Task<Result<T>> CreateAsync([NotNull] DBProvider provider, [NotNull] IUserSessionService userSessionService,
            [NotNull] TCreate model, CancellationToken cancellationToken = default)
        {
            var result = new Result<T>(_logger);

            try
            {
                if (ValidCreateRight != null)
                {
                    await ValidCreateRight(result, provider, model, userSessionService, cancellationToken);

                    if (!result.Success)
                        return result;
                }

                if (ValidCreateModel != null)
                {
                    await ValidCreateRight(result, provider, model, userSessionService, cancellationToken);

                    if (!result.Success)
                        return result;
                }

                if (PrepareCreate == null)
                    return result.AddError("Ошибка создания", "Подготовка записи не реализована", 1);

                var entity = await PrepareCreate(result, provider, model, userSessionService, cancellationToken);

                if (!result.Success)
                    return result;

                if (entity == null)
                    return result.AddError("Ошибка создания. Попробуйте снова. Обратитесь к администратору", "Метод создания отработал успешно, но сущность null", 2);

                var addedResult = await provider.DB.AddEntityAsync(entity, cancellationToken);

                if (!addedResult.Success)
                    return result;

                if (PostCreate != null)
                    await PostCreate(result, provider, model, userSessionService, cancellationToken);

                if (!result.Success)
                    return result;

                return result.AddModel(entity);

            }
            catch (TaskCanceledException e)
            {
                return result.AddError(e, "Операция отменена");
            }
            catch (Exception e)
            {
                return result.AddError(e, "Ошибка создания. Попробуйте снова", 3);
            }
        }

        public Task<Result> DeleteAsync([NotNull] DBProvider provider, [NotNull] IUserSessionService userSessionService,
            [NotNull] TDelete model, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Result<T>> UpdateAsync([NotNull] DBProvider provider, [NotNull] IUserSessionService userSessionService,
            [NotNull] TUpdate model, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
