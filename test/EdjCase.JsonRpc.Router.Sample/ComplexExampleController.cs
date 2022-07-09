using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EdjCase.JsonRpc.Router.Sample
{
    /// <summary>
    /// This complex api
    /// </summary>
    [RpcRoute("api/v1/complex")]
    public class ComplexExampleController : ControllerBase
    {
        static ConcurrentDictionary<int, ComplexInputModel> memoryCache = new ConcurrentDictionary<int, ComplexInputModel>();
        
        
        /// <summary>
        /// Save or update model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task Save(ComplexInputModel model)
        {
            await Task.Run(() =>
            {
                ComplexExampleController.memoryCache.AddOrUpdate(model.Id, model, (i, inputModel) =>
                {
                    inputModel.Title = model.Title;
                    inputModel.CreatedOn = model.CreatedOn;
                    inputModel.IsEnabled = model.IsEnabled;
                    
                    return inputModel;
                });
            });
        }
        
        
        /// <summary>
        /// Get model by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ComplexInputModel> Get(int id)
        {
            return await Task.Run(() =>
            {
                ComplexExampleController.memoryCache.TryGetValue(id, out var model);
                return model;
            });
        }
        
        
        /// <summary>
        /// Get all models
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<ComplexInputModel>> Get()
        {
            return await Task.Run(() => ComplexExampleController.memoryCache.Select(x=>x.Value));
        }
    }

    public class ComplexInputModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}