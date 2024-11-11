
using BaseLibrary.Responses;
using ClientCommon.Services.Contracts;
using ClientLibrary.Helpers;
using System.Net.Http;
using System.Net.Http.Json;

namespace ClientCommon.Services.Implementations;

public class GenericServiceImplementation<T>(GetHttpClient getHttpClient) : IGenericServiceInterface<T>
{
    //create 
    public async Task<GeneralResponse> Insert(T item, string baseUrl)
    {
        var httpClient = await getHttpClient.GetPrivateHttpClient();
        var response = await httpClient.PostAsJsonAsync($"{baseUrl}/add", item);
        var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
        return result!;
    }

    //read all
    public async Task<List<T>> GetAll(string baseUrl)
    {
        var httpClient = await getHttpClient.GetPrivateHttpClient();
        var result = await httpClient.GetFromJsonAsync<List<T>>($"{baseUrl}/all");
        return result!;
    }

    // read Single {id}
    public async Task<T> GetById(int id, string baseUrl)
    {
        var httpClient = await getHttpClient.GetPrivateHttpClient();
        var result = await httpClient.GetFromJsonAsync<T>($"{baseUrl}/single/{id}");
        return result!;
    }

    // update {model}
    public async Task<GeneralResponse> Update(T item, string baseUrl)
    {
        var httpClient = await getHttpClient.GetPrivateHttpClient();
        var response = await httpClient.PutAsJsonAsync($"{baseUrl}/update", item);
        var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
        return result!;
    }

    // delete {id}
    public async Task<GeneralResponse> DeleteById(int id, string baseUrl)
    {
        var httpClient = await getHttpClient.GetPrivateHttpClient();
        var response = await httpClient.DeleteAsync($"{baseUrl}/delete/{id}");
        var result = await response.Content.ReadFromJsonAsync<GeneralResponse>();
        return result!;
    }
}
