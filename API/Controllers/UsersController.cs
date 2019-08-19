﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using API.Models.ViewModels;
using API.Repositories.Interfaces;
using API.Email.Interfaces;
using System.Net;

namespace API.Controllers
{
    [ApiController]
    [ValidateModel]
    [Route("api/v1/[controller]")]
    public class UsersController : Controller
    {
        private readonly IEmail email;
        private readonly IUserRepository userRepository;
        private readonly ILogger<UsersController> logger;

        public UsersController(IEmail email,
                               ILogger<UsersController> logger,
                               IUserRepository userRepository)
        {
            this.email = email ?? throw new ArgumentNullException(nameof(email));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        [HttpGet("test")]
        public string Test() => "This is a test endpoint";

        [HttpPost("registration-complete/{token}")]
        public async Task<IActionResult> RegistrationComplete(string token)
        {
            var valid = await userRepository.IsEmailConfirmationValid(token);

            if (!valid) {
                return BadRequest("Sorry this request is invalid");
            }

            return Created("api/v1/users/registration-complete", "Your email has been confirmed");
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegiserViewModel data)
        {
           if(userRepository.DoesUsernameExist(data.UserName)) {
                return BadRequest("Sorry this username has already been used");
           }

           if(userRepository.DoesEmailExist(data.UserName)) {
                return BadRequest("Sorry this email address has already been used");
           }

            var result = await userRepository.Create(data.UserName, data.Email, data.Password);

            if(result == null)
            {
                logger.Log(LogLevel.Error, "User: " + data.UserName + " hasn't been registered");
                return BadRequest("Sorry you can't be registered at the moment");
            }

            var emailResult = await email.SendCofirmationEmail(result.Username, 
                                                               result.Email, 
                                                               result.EmailConfirmationToken);

            if(emailResult != HttpStatusCode.OK) {
                logger.Log(LogLevel.Error, "User: " + data.UserName + "registration email hasn't been sent");
            }

            return Created("api/v1/users/register", "You've been registered");
        }
    }
}