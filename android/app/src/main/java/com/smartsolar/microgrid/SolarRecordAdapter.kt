package com.smartsolar.microgrid

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.smartsolar.microgrid.databinding.ItemSolarRecordBinding

data class SolarRecord(val id: String, val title: String, val body: String, val status: String,
    val action: String, val enabled: Boolean = true, val onClick: () -> Unit)

class SolarRecordAdapter : ListAdapter<SolarRecord, SolarRecordAdapter.Holder>(object : DiffUtil.ItemCallback<SolarRecord>() {
    override fun areItemsTheSame(old: SolarRecord, new: SolarRecord) = old.id == new.id
    override fun areContentsTheSame(old: SolarRecord, new: SolarRecord) = old == new
}) {
    class Holder(val binding: ItemSolarRecordBinding) : RecyclerView.ViewHolder(binding.root)
    override fun onCreateViewHolder(parent: ViewGroup, type: Int) = Holder(ItemSolarRecordBinding.inflate(LayoutInflater.from(parent.context), parent, false))
    override fun onBindViewHolder(holder: Holder, position: Int) { holder.binding.bindSolarRecord(getItem(position)) }
}

fun ItemSolarRecordBinding.bindSolarRecord(record: SolarRecord) {
    recordTitle.text = record.title
    recordBody.text = record.body
    recordStatus.solarStatus(record.status)
    recordAction.text = record.action
    recordAction.isEnabled = record.enabled
    recordAction.setOnClickListener { record.onClick() }
}
