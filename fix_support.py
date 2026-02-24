path = 'd:/VS REPO/ClientSphere/Views/SupportStaff/Dashboard.cshtml'
with open(path, encoding='utf-8') as f:
    c = f.read()

old = '''                                <td class="text-end pe-4">
                                    <form asp-action="ResolveTicket" asp-route-id="@ticket.Id" method="post">
                                        <button type="button" class="btn btn-sm btn-outline-secondary me-1"><i class="bi bi-pencil"></i></button>
                                        <button type="submit" class="btn btn-sm btn-success text-white" title="Mark as Resolved">
                                            <i class="bi bi-check2"></i> Resolve
                                        </button>
                                    </form>
                                </td>
                            </tr>'''

new = '''                                <td class="text-end pe-4">
                                    <div class="d-flex gap-1 justify-content-end">
                                        <button type="button" class="btn btn-sm btn-outline-secondary"
                                            data-bs-toggle="modal" data-bs-target="#commentModal-@ticket.Id">
                                            <i class="bi bi-pencil"></i> Comment
                                        </button>
                                        <form asp-action="CloseTicket" asp-route-id="@ticket.Id" method="post" class="d-inline">
                                            @Html.AntiForgeryToken()
                                            <button type="submit" class="btn btn-sm btn-danger text-white">
                                                <i class="bi bi-x-circle"></i> Close
                                            </button>
                                        </form>
                                        <form asp-action="ResolveTicket" asp-route-id="@ticket.Id" method="post" class="d-inline">
                                            @Html.AntiForgeryToken()
                                            <button type="submit" class="btn btn-sm btn-success text-white">
                                                <i class="bi bi-check2"></i> Resolve
                                            </button>
                                        </form>
                                    </div>
                                </td>
                            </tr>
                            <!-- Comment Modal -->
                            <div class="modal fade" id="commentModal-@ticket.Id" tabindex="-1">
                                <div class="modal-dialog">
                                    <div class="modal-content">
                                        <form asp-action="AddComment" method="post">
                                            @Html.AntiForgeryToken()
                                            <input type="hidden" name="ticketId" value="@ticket.Id" />
                                            <div class="modal-header">
                                                <h5 class="modal-title fw-bold">Add Comment</h5>
                                                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                                            </div>
                                            <div class="modal-body">
                                                <label class="form-label fw-bold">Your Comment</label>
                                                <textarea name="comment" class="form-control" rows="4" placeholder="Type your update..." required></textarea>
                                            </div>
                                            <div class="modal-footer">
                                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                                                <button type="submit" class="btn btn-primary"><i class="bi bi-send me-1"></i>Submit</button>
                                            </div>
                                        </form>
                                    </div>
                                </div>
                            </div>'''

import re
c2 = re.sub(r'<td class="text-end pe-4">.*?</tr>', new.strip(), c, flags=re.DOTALL, count=1)
# append correct closing </tr> after modal
# Actually just replace directly since we included </tr> in new
with open(path, 'w', encoding='utf-8') as f:
    f.write(c2)
print('Done')
