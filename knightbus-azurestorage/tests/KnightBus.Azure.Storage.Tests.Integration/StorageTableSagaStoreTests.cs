﻿using System;
using System.Threading.Tasks;
using FluentAssertions;
using KnightBus.Azure.Storage.Sagas;
using KnightBus.Core.Sagas;
using KnightBus.Core.Sagas.Exceptions;
using NUnit.Framework;

namespace KnightBus.Azure.Storage.Tests.Integration
{
    [TestFixture]
    public class StorageTableSagaStoreTests
    {
        private readonly ISagaStore _sagaStore = new StorageTableSagaStore("UseDevelopmentStorage=true");

        private class SagaData
        {
            public string Message { get; set; }
        }

        [Test]
        public async Task Complete_should_delete_the_saga()
        {
            //arrange
            var partitionKey = Guid.NewGuid().ToString("N");
            var id = Guid.NewGuid().ToString("N");
            await _sagaStore.Create(partitionKey, id, new SagaData {Message = "yo"}, TimeSpan.FromMinutes(1));
            //act
            await _sagaStore.Complete(partitionKey, id);
            //assert
            _sagaStore.Awaiting(x => x.GetSaga<SagaData>(partitionKey, id)).Should().Throw<SagaNotFoundException>();
        }

        [Test]
        public void Complete_should_throw_when_saga_not_found()
        {
            //arrange
            var partitionKey = Guid.NewGuid().ToString("N");
            var id = Guid.NewGuid().ToString("N");
            //act & assert
            _sagaStore.Awaiting(x => x.Complete(partitionKey, id)).Should().Throw<SagaNotFoundException>();
        }

        [Test]
        public async Task Should_create_and_read_saga()
        {
            //arrange
            var partitionKey = Guid.NewGuid().ToString("N");
            var id = Guid.NewGuid().ToString("N");

            //act
            await _sagaStore.Create(partitionKey, id, new SagaData {Message = "yo"}, TimeSpan.FromMinutes(1));
            //assert
            var saga = await _sagaStore.GetSaga<SagaData>(partitionKey, id);
            saga.Message.Should().Be("yo");
        }

        [Test]
        public async Task Should_not_throw_when_create_and_saga_expired()
        {
            //arrange
            var partitionKey = Guid.NewGuid().ToString("N");
            var id = Guid.NewGuid().ToString("N");
            await _sagaStore.Create(partitionKey, id, new SagaData {Message = "yo"}, TimeSpan.FromMinutes(-1));
            //act & assert
            _sagaStore.Awaiting(x => x.Create(partitionKey, id, new SagaData {Message = "yo"}, TimeSpan.FromMinutes(1)))
                .Should().NotThrow<SagaAlreadyStartedException>();
        }

        [Test]
        public async Task Should_throw_when_create_and_saga_exists()
        {
            //arrange
            var partitionKey = Guid.NewGuid().ToString("N");
            var id = Guid.NewGuid().ToString("N");
            await _sagaStore.Create(partitionKey, id, new SagaData {Message = "yo"}, TimeSpan.FromMinutes(1));
            //act & assert
            _sagaStore.Awaiting(x => x.Create(partitionKey, id, new SagaData {Message = "yo"}, TimeSpan.FromMinutes(1)))
                .Should().Throw<SagaAlreadyStartedException>();
        }

        [Test]
        public void Should_throw_when_get_and_saga_does_not_exist()
        {
            //arrange
            var partitionKey = Guid.NewGuid().ToString("N");
            var id = Guid.NewGuid().ToString("N");
            //act & assert
            _sagaStore.Awaiting(x => x.GetSaga<SagaData>(partitionKey, id)).Should().Throw<SagaNotFoundException>();
        }

        [Test]
        public async Task Should_throw_when_get_and_saga_expired()
        {
            //arrange
            var partitionKey = Guid.NewGuid().ToString("N");
            var id = Guid.NewGuid().ToString("N");
            await _sagaStore.Create(partitionKey, id, new SagaData {Message = "yo"}, TimeSpan.FromMinutes(-1));
            //act & assert
            _sagaStore.Awaiting(x => x.GetSaga<SagaData>(partitionKey, id)).Should().Throw<SagaNotFoundException>();
        }

        [Test]
        public void Update_should_throw_when_saga_not_found()
        {
            //arrange
            var partitionKey = Guid.NewGuid().ToString("N");
            var id = Guid.NewGuid().ToString("N");
            //act & assert
            _sagaStore.Awaiting(x => x.Update(partitionKey, id, new SagaData())).Should().Throw<SagaNotFoundException>();
        }

        [Test]
        public async Task Update_should_update_the_saga()
        {
            //arrange
            var partitionKey = Guid.NewGuid().ToString("N");
            var id = Guid.NewGuid().ToString("N");
            await _sagaStore.Create(partitionKey, id, new SagaData {Message = "yo"}, TimeSpan.FromMinutes(1));
            //act
            await _sagaStore.Update(partitionKey, id, new SagaData {Message = "updated"});
            //assert
            var data = await _sagaStore.GetSaga<SagaData>(partitionKey, id);
            data.Message.Should().Be("updated");
        }
    }
}