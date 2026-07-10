// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace ForgeKit.Api.Models
{
    public class JwtSetupData
    {
        /// <summary>
        /// This identifies provider that issued the JWT
        /// </summary>
        public required string Issuer { get; set; }
        /// <summary>
        /// This identifies the recipients that the JWT is intended for
        /// </summary>
        public required string Audience { get; set; }
        // /// <summary>
        // /// This is a SECRET key that both the issuer and audience have to have 
        // /// </summary>
        // public required string SigningKey { get; set; }
    }
}