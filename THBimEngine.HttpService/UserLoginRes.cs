using Newtonsoft.Json;
using System.Collections.Generic;

namespace THBimEngine.HttpService
{
    public class UserLoginRes
    {
        public string Username { get; set; }
        public int Id { get; set; }
        [JsonProperty("desk_phone")]
        public string DeskPhone { get; set; }
        public string Email { get; set; }
        [JsonProperty("jwt_token")]
        public string Token { get; set; }
    }
    public class UserInfo
    {
        [JsonIgnore]
        public UserLoginRes UserLogin { get; set; }
        [JsonProperty("position_title")]
        public string PositionTitle { get; set; }
        [JsonProperty("clerk_code")]
        public string ClerkCode { get; set; }
        [JsonProperty("chinese_name")]
        public string ChineseName { get; set; }
        [JsonProperty("pre_sso_id")]
        public string PreSSOId { get; set; }
        [JsonIgnore]
        public string LoginLocation { get; set; }
        public List<Department> Departments { get; set; }
    }
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonProperty("company_name")]
        public string CompanyName { get; set; }
    }
}
