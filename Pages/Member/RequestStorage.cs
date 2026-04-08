using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace ACC_Demo.Pages.Member
{
    public static class RequestStorage
    {
        private static List<HelpRequest> _requests = new List<HelpRequest>();
        private static int _nextId = 1;

        public static void AddRequest(HelpRequest request)
        {
            _requests.Add(request);
        }

        public static List<HelpRequest> GetAllRequests()
        {
            return _requests.ToList();
        }

        public static HelpRequest GetRequestById(int id)
        {
            return _requests.FirstOrDefault(r => r.Id == id);
        }

        public static int GetNextId()
        {
            return _nextId++;
        }
    }
}