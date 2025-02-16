using System;
using System.IO;
using System.Security.Cryptography;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Blocks;
using Libplanet.Store;
using NineChronicles.Headless.Executable.Store;
using Xunit;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Headless.Executable.Tests.Store
{
    public class IStoreExtensionsTest : IDisposable
    {
        private readonly string _storePath;

        public IStoreExtensionsTest()
        {
            _storePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        [Theory]
        [InlineData(StoreType.Default)]
        [InlineData(StoreType.RocksDb)]
        public void GetGenesisBlock(StoreType storeType)
        {
            IStore store = storeType.CreateStore(_storePath);
            HashAlgorithmType hashAlgo = HashAlgorithmType.Of<SHA256>();
            Block<NCAction> genesisBlock = BlockChain<NCAction>.MakeGenesisBlock(hashAlgo);
            Guid chainId = Guid.NewGuid();
            store.SetCanonicalChainId(chainId);
            store.PutBlock(genesisBlock);
            store.AppendIndex(chainId, genesisBlock.Hash);

            Assert.Equal(genesisBlock, store.GetGenesisBlock<NCAction>(_ => hashAlgo));

            (store as IDisposable)?.Dispose();
        }

        [Theory]
        [InlineData(StoreType.Default)]
        [InlineData(StoreType.RocksDb)]
        public void GetGenesisBlock_ThrowsInvalidOperationException_IfChainIdNotExist(StoreType storeType)
        {
            IStore store = storeType.CreateStore(_storePath);
            Assert.Throws<InvalidOperationException>(
                () => store.GetGenesisBlock<NCAction>(_ => HashAlgorithmType.Of<SHA256>())
            );
            (store as IDisposable)?.Dispose();
        }

        public void Dispose()
        {
            if (Directory.Exists(_storePath))
            {
                Directory.Delete(_storePath, true);
            }
        }
    }
}
