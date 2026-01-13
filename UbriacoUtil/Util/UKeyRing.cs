/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Solusek Systems. All rights reserved.
 *--------------------------------------------------------------------------------------------*/

using Newtonsoft.Json;

namespace Ubriaco.Util;

public static class UKeyRing
{
    internal static int Size = 109;

    public class _DATA
    {
        public List<string> list { get; set; } = new List<string>();
    }

    public static async Task LoadAsync()
    {
        using (var redis = await RedisUtil.CreateAsync())
        {
            var data = await redis.GetAsync("KEYRING");
            if (!string.IsNullOrEmpty(data))
            {
                try
                {
                    var o = JsonConvert.DeserializeObject<_DATA>(data);
                    if (o != null)
                    {
                        if (o.list.Count == Size)
                        {
                            Console.WriteLine("Found keyring in cache.");
                            for (int n = 0; n < Size; n++)
                            {
                                RingData[n] = o.list[n];
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to load keyring from cache: " + ex.Message);
                }
            }
        }
    }

    public static async Task ShuffleAndStore()
    {
        using (var redis = await RedisUtil.CreateAsync())
        {
            var o = new _DATA();
            for (int n = 0; n < Size; n++)
            {
                var val = Guid.NewGuid().ToString();
                o.list.Add(val);
                RingData[n] = val;
            }
            var data = JsonConvert.SerializeObject(o);
            await redis.PutAsync("KEYRING", data);
            await File.WriteAllTextAsync("KEYRING", data);
        }
    }

    public static string[] RingData = new string[]
    {
        "e823d961-f0a2-4c17-9df3-5c2eb2a839ac",
        "3172999d-1b57-44d4-a9b2-88f1c2c9c2e0",
        "f2e1372e-4ac3-4c6f-9499-d90dd6e2a4a7",
        "3f1a81e0-45ec-421b-96ea-e46b7b3c94c3",
        "244aa511-53d0-4e36-bbc7-8c8b2d4b95e5",
        "071701dc-ec91-4999-bc15-60c3f2902024",
        "a10ac130-7c36-45ff-9be3-e3030a2cb7b5",
        "8ab04fe6-1613-4e61-8415-2704e8326452",
        "b39378da-3fd3-4eb4-9f76-32b93b8f2487",
        "2e8a6df7-3ab7-45d7-b37d-731167db108b",
        "7589ae1b-8a2d-4384-8a00-2a009f88fa63",
        "f96ce057-4176-4f3a-81aa-b85a6c091f13",
        "59f66700-85ff-4a06-b1f5-65f2c8f77cf1",
        "9f1d78e6-10b5-4eae-a9e5-98f2ad76cf93",
        "b7405dfb-1304-4c36-a490-c169d82d0b5a",
        "20a9bb7d-305b-4f7b-90a5-e315d7b77912",
        "79c7ae7a-c36b-42aa-b7f8-967964ec29b3",
        "6611b5dc-1b2f-4cd7-b69c-b17d58f43bcd",
        "67ad7ec7-e7b6-4e27-9f3c-1034d43b7f92",
        "e94b6c48-d024-40db-8658-898eb6236b33",
        "71c8f0fc-80c8-4454-bf5c-36fe622f8f31",
        "3a5e66e4-050b-4d83-8261-4cfcffec16c6",
        "3f6d2d8f-5894-4304-85f2-20e7929e980e",
        "4f8bb7a3-e938-4ee5-b6a2-e846fd292b40",
        "78c82fd2-6e38-42cd-b116-938d2a78e690",
        "af374d32-fbd8-4e2e-96cb-e2f2e44217f0",
        "8f805eff-bf89-4d6a-8704-b02131b8a91a",
        "0a7d786f-1d5f-46bb-8a85-00c4e09de153",
        "9d1b8203-f9e8-4e32-b6e3-9518d23e0d48",
        "7beaa8b4-9b49-4019-a18f-1f7d4f9074d8",
        "0d2c5d80-df23-4de1-a9bc-46dd92c46958",
        "b4a1df11-306b-4dc2-a709-d9fd26985d11",
        "02deeb85-3c2e-4f3f-a08b-d94b5f4cfb34",
        "e4a4041d-061f-4e94-91fc-9c3d383a6f59",
        "113f4569-fb83-4e36-840e-f2ea27ea9af1",
        "79619199-f09a-407f-a78e-753a2a9c5ec4",
        "7e799d03-8a2b-4384-81ff-7cc5e038db02",
        "d4e5486b-d3f1-40ff-91a0-4e39a71c9370",
        "0ccfe1f0-304d-4b11-b939-24b9de6f1249",
        "2e96199e-cf83-40f5-b0b3-2340051ad6c6",
        "f5278c7f-b258-48a9-bbff-2ed727c97b52",
        "31f8bc1f-7869-4f39-bf2d-8c27dc1c7a59",
        "35a17f43-e7fd-4262-89bb-6e158a72e658",
        "8be44d1a-4199-4016-8e5f-f4f2b4adf30c",
        "f7c775d9-d7ff-4324-a3cc-d80b07ef4b6c",
        "1ecdd504-4b91-4f59-a55c-81e3f0302ed6",
        "3f702a2a-d4d0-409f-9392-0fc68f6f3f9f",
        "aac7b3db-22cf-4c7e-b320-c07b4a81cf4a",
        "cf1620e7-2f5e-4b4e-a279-8be93771d1a6",
        "b5975d17-94f2-4a8f-bd94-dc3772ff8db7",
        "8e2b6a13-7ed0-4c02-bb9e-b3ad42c3ad8e",
        "e3e6bc7a-3207-43c3-84d8-e195df86c68f",
        "bc96e0e1-d1f4-40f6-baa3-bad39e30e216",
        "e13d5c06-f973-4ff5-9811-b09c7e3e6df3",
        "ccfe0a49-72b6-46ce-a9ad-6ce3c75d1a7a",
        "0a244bb3-4f02-4e63-981a-2e8fa84c7029",
        "c42a3f2c-3c3e-47b6-80ed-470d7df147a0",
        "c38f8bb8-0c2f-4010-81b1-6dbfd3262f9c",
        "2a803177-3b83-4a6a-acc9-df11738d8b0c",
        "7debf276-d84a-45f2-a364-8585cc174d42",
        "13f0bc95-27e4-4747-8f3a-1b7f926c8f11",
        "b3d998c1-cf33-47b0-bc38-3ae5733604a0",
        "ecb93556-b1f2-49fa-9a48-b124e8dd89e3",
        "e79a02e6-5f62-4e96-b60a-0ab45dc258ec",
        "fef2087a-5c83-421c-9359-3a4cbec6f7cf",
        "73c1f0a0-cc38-4ad2-8c00-e7f3a3ea4ec5",
        "2a1be0b0-86e3-49d3-a938-fb5d29f94b89",
        "59a249e4-309e-4b66-97e8-5f158e3c91da",
        "3f5fc2ea-9860-44ec-899c-d50d3821a4c4",
        "36a9d1f3-5df8-4a19-9617-60b21b74f3a7",
        "ff60eaca-9c5f-43fa-aaa7-e5b9d969d761",
        "cf6d10f0-58cc-419e-b49b-02d518cbeb20",
        "fdedbb2e-c7a7-47a6-ae13-62a0e4a8b5b6",
        "d61a5f82-e47e-4907-800a-2caa264c2795",
        "47a65113-f4b8-420a-8968-f7e493de2f46",
        "de91cb9d-4051-46c4-b441-1f6a9ab2f383",
        "a5bc2a64-3b7b-4b01-bf90-b70d0cf69ad7",
        "d37f1cf5-6b3f-4d57-b3f4-3fcdf72cdb2f",
        "a9822c04-74a1-43c4-a8a1-dcb5a2e49893",
        "b19490bc-4608-4be4-80b7-e39d871c370f",
        "ff53f1cc-3cbb-4e6e-a96a-26129c51eb6f",
        "79b3d969-e812-4c8d-99de-3c3f4377adcb",
        "ffbdc5c4-fd7a-40db-a305-69b4d32193d1",
        "8e5196f4-4f9e-49c4-a015-4a3969f52e3d",
        "765da4e7-1d7d-4033-b803-71995b4488f3",
        "d26b5c70-d2ce-4b3d-867b-663c16e994f4",
        "dbf8f2b1-6633-4b59-a949-53c8c924c9d7",
        "e050a9c1-ef20-4fd2-bbfa-44819192d19e",
        "4fc6c086-c48f-4074-b5c3-0f5ff48706a1",
        "f080c76d-6bbf-4808-a1f9-c63fbdba561b",
        "d2e4729d-6d52-49a2-b309-b033f2df77b3",
        "aa7249cf-97ce-4a5c-a44f-6b6aece0ff2e",
        "3b181b63-0df2-42c4-a540-65ad1549b634",
        "92b63312-4a43-4f8a-91ae-6fbb7aa17ec7",
        "f8bb18f4-0db9-437f-8172-c27583c61b2c",
        "e973d819-d960-4345-a50d-6ad26b79e091",
        "c79cfe91-11cf-4ff2-8c5b-7d38b274994c",
        "f19f2922-2412-4e7d-860a-f0ea8e0577db",
        "40823b5d-47da-4f12-8a88-83d5a23de746",
        "34952d59-11d2-4b18-9910-2f8d1e7e6b4a",
        "b822e40e-5e8b-4a5a-a4e6-b301ecb5aa0f",
        "6ae982da-e7d0-4343-932f-0d6b9e67ae2a",
        "b11f71aa-38cb-49f1-8938-6d489d6f62b0",
        "7e9c6357-0df5-4760-bd4f-1b3c2db3ea99",
        "1b9b9825-2c65-4cb8-8fc6-4ff23cf49da6",
        "52ec0464-0f43-46fc-9e6f-2220f1b95a3a",
        "b0c7206b-00cc-4d30-9130-566348fd3f82",
        "3ce7db0d-27dc-4c3d-9ae3-75ef49fdb4ad",
        "43593571-7b3f-4e8b-9f10-53b5c359d8c6"
    };

}