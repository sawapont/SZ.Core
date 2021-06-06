﻿using Al;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;

using SZ.Core.Models.Configurations;
using SZ.Core.Models.Interfaces;

namespace SZ.Core.Models.Db
{
    public class SZDb :
        IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        /// <summary>
        /// Документы
        /// </summary>
        public DbSet<Document> Documents { get; set; }
        /// <summary>
        /// Решения в документах
        /// </summary>
        public DbSet<DocumentDecision> DocumentDecisions { get; set; }
        /// <summary>
        /// Пользователи документов
        /// </summary>
        public DbSet<DocumentUser> DocumentUsers { get; set; }
        /// <summary>
        /// Должности земства
        /// </summary>
        public DbSet<Position> Positions { get; set; }
        /// <summary>
        /// Повторы вопросов в протоколах
        /// </summary>
        public DbSet<ProtocolQuestionRepeat> ProtocolQuestionRepeats { get; set; }
        /// <summary>
        /// Генерируемые пользователями вопросы
        /// </summary>
        public DbSet<Question> Questions { get; set; }
        /// <summary>
        /// Заявленные варианты ответов на вопросы
        /// </summary>
        public DbSet<QuestionRepeatAnswer> QuestionRepeatAnswers { get; set; }
        /// <summary>
        /// Десятки
        /// </summary>
        public DbSet<Ten> Tens { get; set; }
        /// <summary>
        /// Пользователи, назначенные на должности
        /// </summary>
        public DbSet<ZemstvoUserPosition> ZemstvoUserPositions { get; set; }
        /// <summary>
        /// Состав десяток
        /// </summary>
        public DbSet<UserTen> UserTens { get; set; }
        /// <summary>
        /// Земства
        /// </summary>
        public DbSet<Zemstvo> Zemstvos { get; set; }
        /// <summary>
        /// Повторы обсуждения вопросов
        /// </summary>
        public DbSet<QuestionRepeat> QuestionRepeats { get; set; }

        readonly ILogger _logger;
        readonly bool _withoutMigrations;

        public SZDb(DbContextOptions<SZDb> options, ILoggerFactory loggerFactory = null, bool withoutMigrations = false) : base(options)
        {
            _logger = loggerFactory?.CreateLogger<SZDb>();
            _withoutMigrations = withoutMigrations;
        }

        public async ValueTask<Result> AddEntityAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class, IDBEntity
        {
            var result = new Result(_logger);
            var saveCounter = 5;
            try
            {
                var dbset = Set<T>();

                await dbset.AddAsync(entity, cancellationToken);

                // даётся 5 попыток записать сущность
                do
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new TaskCanceledException();

                    int maxId;

                    if (await dbset.AnyAsync(cancellationToken))
                        maxId = await dbset.MaxAsync(x => x.ShowId, cancellationToken);
                    else
                        maxId = 0;

                    try
                    {
                        entity.ShowId = ++maxId;

                        await SaveChangesAsync(cancellationToken);

                        return result;
                    }
                    catch (TaskCanceledException e)
                    {
                        return result.AddError(e, "Операция отменена");
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning($"Неудачная попытка записать сущность {typeof(T).Name} id={entity.Id} c showId {maxId}");
                    }
                    saveCounter--;
                } while (saveCounter > 0);
            }
            catch (TaskCanceledException e)
            {
                return result.AddError(e, "Операция отменена");
            }
            catch (Exception e)
            {
                return result.AddError(e, "Ошибка добавления сущности", 1, LogLevel.Error);
            }

            return result.AddError($"Не удалось выполнить операцию с сущностью {typeof(T).Name} id={entity.Id} после {saveCounter} попыток");
        }

        public struct LengthRequirements
        {
            public struct Users
            {
                public const int FirstName = 100;
                public const int SecondName = FirstName;
                public const int Patronym = FirstName;
                public const int Region = 200;
                public const int Room = 6;
                public const int Street = 200;
                public const int City = 200;
                public const int House = 5;
                public const int Building = 5;
                public const int Flat = 5;
            }
        }

        protected sealed override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new DocumentConfiguration());
            builder.ApplyConfiguration(new DocumentDecisionConfiguration());
            builder.ApplyConfiguration(new DocumentUserConfiguration());
            builder.ApplyConfiguration(new PositionConfiguration());
            builder.ApplyConfiguration(new ProtocolQuestionRepeatConfiguration());
            builder.ApplyConfiguration(new QuestionRepeatAnswerConfiguration());
            builder.ApplyConfiguration(new QuestionConfiguration());
            builder.ApplyConfiguration(new QuestionRepeatConfiguration());
            builder.ApplyConfiguration(new TenConfiguration());
            builder.ApplyConfiguration(new UserConfiguration());
            builder.ApplyConfiguration(new UserTenConfiguration());
            builder.ApplyConfiguration(new ZemstvoConfiguration());
            builder.ApplyConfiguration(new ZemstvoUserPositionConfiguration());

            base.OnModelCreating(builder);

            InitilizeDb.Init(builder);
        }
    }
}
