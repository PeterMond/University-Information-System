﻿namespace UniversityInformationSystem.App.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices.ComTypes;
    using System.Web.Mvc;
    using Exceptions;
    using Models.EntityModels.Users;
    using Models.ViewModels.Messages;
    using PagedList;
    using Services.Contracts;

    [RoutePrefix("messages")]
    [Authorize]
    public class MessagesController : Controller
    {
        private const int pageSize = 5;
        private IMessagesService messagesService;

        public MessagesController(IMessagesService messagesService)
        {
            this.messagesService = messagesService;
        }

        [Route("create")]
        [HttpGet]
        public ActionResult Create()
        {
            return this.View();
        }

        [Route("create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateMessageViewModel messageVm)
        {
            if (this.ModelState.IsValid)
            {
                string senderUsername = HttpContext.User.Identity.Name;
                this.messagesService.Create(senderUsername, messageVm.ReceiverUsername, messageVm.Text);
                return this.RedirectToAction("SuccessfulCreate", new { receiverUsername = messageVm.ReceiverUsername});
            }

            return this.View();
        }

        [Route("success")]
        [HttpGet]
        public ActionResult SuccessfulCreate(string receiverUsername)
        {
            if (receiverUsername == null)
            {
                return this.RedirectToAction("Create");
            }

            CreateMessageSuccessViewModel vm = new CreateMessageSuccessViewModel() { ReceiverUsername = receiverUsername};
            return this.View(vm);
        }

        [Route("inbox")]
        [HttpGet]
        public ActionResult Inbox(int? page)
        {
            string username = HttpContext.User.Identity.Name;
            IEnumerable<InboxMessageViewModel> inboxMessages = this.messagesService.InboxMessages(username);

            int pageNumber = page ?? 1;
            return this.View(inboxMessages.ToPagedList(pageNumber, pageSize));
        }

        [Route("delete/{id}")]
        [HttpGet]
        public ActionResult Delete(int id)
        {
            if (!this.messagesService.MessageExists(id))
            {
                throw new NonExistingMessageException();
            }

            if (this.messagesService.UnauthorizedDelete(id, HttpContext.User.Identity.Name))
            {
                throw new UnauthorizedDeleteException();
            }

            InboxMessageViewModel messageVm = this.messagesService.GetInboxMessageById(id);

            return this.View(messageVm);
        }

        [Route("delete/{id}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (this.messagesService.MessageExists(id))
            {
                this.messagesService.Delete(id);
                return RedirectToAction("Inbox");
            }

            throw new NonExistingMessageException();
        }

        [Route("usernames")]
        public ActionResult GetAllUsernames(string username)
        {
            if (username == null)
            {
                return RedirectToAction("Create");
            }

            string currentUsername = HttpContext.User.Identity.Name;
            string[] allUsernames = this.messagesService
                .GetAllUsernames(currentUsername)
                .Where(u => u.ToLower().Contains(username.ToLower()))
                .ToArray();

            return this.Json(allUsernames, JsonRequestBehavior.AllowGet);
        }
    }
}