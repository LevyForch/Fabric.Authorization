﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Authorization.Domain.Exceptions;
using Fabric.Authorization.Domain.Models;
using Fabric.Authorization.Domain.Services;
using Fabric.Authorization.Domain.Stores;
using Fabric.Authorization.Persistence.CouchDb.Services;
using Serilog;

namespace Fabric.Authorization.Persistence.CouchDb.Stores
{
    public class CouchDbGroupStore : FormattableIdentifierStore<string, Group>, IGroupStore
    {
        private readonly IRoleStore _roleStore;
        private readonly IUserStore _userStore;
        private const string IdDelimiter = "-:-:";

        public CouchDbGroupStore(
            IDocumentDbService dbService,
            ILogger logger,
            IEventContextResolverService eventContextResolverService,
            IIdentifierFormatter identifierFormatter, 
            IRoleStore roleStore,
            IUserStore userStore) : base(dbService, logger, eventContextResolverService, identifierFormatter)
        {
            _roleStore = roleStore;
            _userStore = userStore;
        }

        public Task<Group> Get(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Exists(Guid id)
        {
            throw new NotImplementedException();
        }

        public override Task<Group> Add(Group group)
        {
            throw new NotImplementedException();
        }

        public override async Task<Group> Get(string id)
        {
            try
            {
                return await base.Get(FormatId(id)).ConfigureAwait(false);
            }
            catch (NotFoundException<Group>)
            {
                Logger.Debug($"Exact match for Group {id} not found.");

                // now attempt to find a group that starts with the supplied ID
                var groups = await DocumentDbService.GetDocuments<Group>(GetGroupIdPrefix(id));
                var activeGroup = groups.FirstOrDefault(g => !g.IsDeleted);
                if (activeGroup == null)
                {
                    throw new NotFoundException<Group>($"Could not find {typeof(Group).Name} entity with ID {id}");
                }

                return activeGroup;
            }
        }

        public override async Task<IEnumerable<Group>> GetAll()
        {
            return await DocumentDbService.GetDocuments<Group>("group");
        }

        public override async Task Delete(Group group)
        {
            await Delete(group.Id.ToString(), group);
        }

        public override async Task Update(Group group)
        {
            group.Track(false, GetActor());
            var activeGroup = await Get(group.Id).ConfigureAwait(false);
            await ExponentialBackoff(DocumentDbService.UpdateDocument(FormatId(activeGroup.Id.ToString()), group));
        }

        protected override async Task Update(string id, Group group)
        {
            group.Track(false, GetActor());
            var activeGroup = await Get(id).ConfigureAwait(false);
            await ExponentialBackoff(DocumentDbService.UpdateDocument(FormatId(activeGroup.Id.ToString()), group));
        }

        public override async Task<bool> Exists(string id)
        {
            try
            {
                var result = await Get(id).ConfigureAwait(false);
                return result != null;
            }
            catch (NotFoundException<Group>)
            {
                Logger.Debug("Exists check failed for Group {id}");
                return false;
            }
        }

        private string GetGroupIdPrefix(string id)
        {
            return $"{DocumentKeyPrefix}{FormatId(id)}{IdDelimiter}";
        }

        public Task<Group> DeleteRolesFromGroup(Group group, IEnumerable<Guid> roleIds)
        {
            throw new NotImplementedException();
        }

        public async Task<Group> AddUserToGroup(Group group, User user)
        {
            if (user.Groups.All(g => !string.Equals(g.Name, group.Name, StringComparison.OrdinalIgnoreCase)))
            {
                user.Groups.Add(group);
            }

            await _userStore.Update(user);
            await Update(group);
            return group;
        }

        public async Task<Group> DeleteUserFromGroup(Group group, User user)
        {
            if (user.Groups.Any(g => string.Equals(g.Name, group.Name, StringComparison.OrdinalIgnoreCase)))
            {
                user.Groups.Remove(group);
            }

            await _userStore.Update(user);
            await Update(group);
            return group;
        }

        public Task<IEnumerable<Group>> GetGroups(string name, string type)
        {
            throw new NotImplementedException();
        }

        public Task<Group> AddRolesToGroup(Group @group, IEnumerable<Role> rolesToAdd)
        {
            throw new NotImplementedException();
        }

        public Task<Group> AddUsersToGroup(Group @group, IEnumerable<User> usersToAdd)
        {
            throw new NotImplementedException();
        }
    }
}